using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YKEnroll.Lib
{
    public class DecodedCsr
    {
        public DecodedCsr(string data)
        {
            Decode(data);
        }
        
        public void Decode(string data)
        {
            ParseCsr(data);
            DecodeCertificateRequest(CsrData!);
            ValidateKeys();
        }

        /// <summary>
        ///     This method will validate that the public key
        ///     in the CSR matches the public key in the 
        ///     Attestation Statement Certificate (If there is one).
        /// </summary>
        private void ValidateKeys()
        {
            if (PublicKey != null && AttestationStatement != null)
            {
                byte[] csr_pubKey = PublicKey.ExportSubjectPublicKeyInfo();
                byte[] att_pubKey = AttestationStatement.StatementCertificate.PublicKey.ExportSubjectPublicKeyInfo();

                if (csr_pubKey.SequenceEqual(att_pubKey))
                    MatchesAttestation = true;
                else
                    MatchesAttestation = false;
            }
        }

        /// <summary>
        ///     This method extracts subject information from a CSR.
        ///     It will extract:
        ///     Subject
        ///     Subject Alternative Name
        ///     Sid (OID_NTDS_CA_SECURITY_EXT)
        ///
        ///     It will also set the CertificateRequest property but
        ///     this object is only partial and does not contain
        ///     the signature from the CSR data and thus cannot
        ///     be used to request a certificate.
        ///
        ///     https://stackoverflow.com/a/63834551
        /// </summary>
        /// <param name="data"></param>
        /// <exception cref="InvalidOperationException"></exception>
        private void DecodeCertificateRequest(string data)
        {
            ReadOnlyMemory<byte> derData = CertHelper.ConvertToDer(data).AsMemory();
            AsnReader asnReader = new AsnReader(derData, AsnEncodingRules.DER);
            AsnReader asnCertReq = asnReader.ReadSequence();
            asnReader.ThrowIfNotEmpty();

            AsnReader asnCertReqInfo = asnCertReq.ReadSequence();
            AsnReader asnAlgorithm = asnCertReq.ReadSequence();
            byte[] signature = asnCertReq.ReadBitString(out int unused);

            if (unused != 0)
            {
                throw new InvalidOperationException("The signature was not complete bytes.");
            }

            asnCertReq.ThrowIfNotEmpty();

            string algorithmOid = asnAlgorithm.ReadObjectIdentifier();

            RSASignaturePadding? signaturePadding = null;

            switch (algorithmOid)
            {
                case "1.2.840.10045.4.3.2": // <- ECCP256
                    HashAlgorithmName = HashAlgorithmName.SHA256;
                    PublicKey = ECDsa.Create();
                    break;
                case "1.2.840.10045.4.3.3": // <- ECCP384
                    HashAlgorithmName = HashAlgorithmName.SHA384;
                    PublicKey = ECDsa.Create();
                    break;
                case "1.2.840.113549.1.1.10": // <- RSA2048
                    HashAlgorithmName = HashAlgorithmName.SHA256;
                    signaturePadding = RSASignaturePadding.Pss;
                    PublicKey = RSA.Create();
                    break;
                default:
                    throw new InvalidOperationException($"No support for signature algorithm '{algorithmOid}'");
            }

            if (asnAlgorithm.HasData && algorithmOid == "1.2.840.113549.1.1.10")
            {
                asnAlgorithm.ReadEncodedValue(); //Read the padding
            }

            asnAlgorithm.ThrowIfNotEmpty();

            if (!asnCertReqInfo.TryReadInt32(out int version) || version != 0)
            {
                throw new InvalidOperationException("Only V1 requests are supported.");
            }

            byte[] encodedSubject = asnCertReqInfo.ReadEncodedValue().ToArray();
            X500DistinguishedName subject = new X500DistinguishedName(encodedSubject);
            Subject = subject.Name;

            byte[] spki_b = asnCertReqInfo.PeekEncodedValue().ToArray();
            AsnReader spki = asnCertReqInfo.ReadSequence();
            AsnReader reqAttrs = asnCertReqInfo.ReadSetOf(new Asn1Tag(TagClass.ContextSpecific, 0));
            asnCertReqInfo.ThrowIfNotEmpty();

            PublicKey.ImportSubjectPublicKeyInfo(spki_b, out int bytesRead);

            if (algorithmOid == "1.2.840.113549.1.1.10")
                CertificateRequest = new CertificateRequest(subject, (RSA)PublicKey, HashAlgorithmName, signaturePadding);
            else
                CertificateRequest = new CertificateRequest(subject, (ECDsa)PublicKey, HashAlgorithmName);

            if (reqAttrs.HasData)
            {
                AsnReader asnAttribute = reqAttrs.ReadSequence();
                string attrType = asnAttribute.ReadObjectIdentifier();
                AsnReader asnAttrValues = asnAttribute.ReadSetOf();

                if (attrType != "1.2.840.113549.1.9.14")
                {
                    throw new InvalidOperationException($"Certification Request attribute '{attrType}' is not supported.");
                }

                asnAttribute.ThrowIfNotEmpty();

                AsnReader asnExtensions = asnAttrValues.ReadSequence();
                asnAttrValues.ThrowIfNotEmpty();

                while (asnExtensions.HasData)
                {
                    AsnReader extension = asnExtensions.ReadSequence();
                    string extensionOid = extension.ReadObjectIdentifier();

                    string[] supportedExtensions = { "2.5.29.17", "1.3.6.1.4.1.311.25.2" };

                    if (!supportedExtensions.Contains(extensionOid))
                    {
                        Logger.Log($"Unsupported extension, oid={extensionOid}");
                        continue;
                    }

                    bool critical = false;
                    byte[] extnValue;

                    if (extension.PeekTag().HasSameClassAndValue(Asn1Tag.Boolean))
                    {
                        critical = extension.ReadBoolean();
                    }

                    extnValue = extension.ReadOctetString();
                    extension.ThrowIfNotEmpty();

                    X509Extension ext = new X509Extension(extensionOid, extnValue, critical);

                    if (CryptoConfig.CreateFromName(extensionOid) is X509Extension typedExtn)
                    {
                        typedExtn.CopyFrom(ext);
                        ext = typedExtn;
                    }
                    CertificateRequest.CertificateExtensions.Add(ext);
                }

                foreach (var e in CertificateRequest.CertificateExtensions)
                {
                    AsnEncodedData extnData = new AsnEncodedData(e.Oid, e.RawData);
                    if (extnData.Oid!.Value == "1.3.6.1.4.1.311.25.2")
                    {
                        Sid = Encoding.UTF8.GetString(extnData.RawData, 20, extnData.RawData.Length - 20);
                    }
                    else if (extnData.Oid.Value == "2.5.29.17")
                    {
                        San = extnData.Format(true).Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    }
                }
            }
            //PemEncoding.Write("PUBLIC KEY", der)
            //ExportSubjectPublicKeyInfoPem()
        }

        /// <summary>
        ///     Parses data from a csr file. If the csr contains an attestation
        ///     statement it will parse this into an AttestationStatement
        ///     object and return it together with the csr. If the csr
        ///     does not contain an attestation if will only return
        ///     the csr.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private void ParseCsr(string data)
        {
            Logger.Log("Attempting to parse csr data.");
            Regex csrRegEx = new Regex("-----BEGIN CERTIFICATE REQUEST-----[\\S\\s]*?-----END CERTIFICATE REQUEST-----");
            Regex statementRegEx = new Regex("-----BEGIN STATEMENT CERTIFICATE-----[\\S\\s]*?-----END STATEMENT CERTIFICATE-----");
            Regex attestationRegEx = new Regex("-----BEGIN ATTESTATION CERTIFICATE-----[\\S\\s]*?-----END ATTESTATION CERTIFICATE-----");

            string csr = csrRegEx.Match(data).Groups[0].ToString();
            string strStatement = statementRegEx.Match(data).Groups[0].ToString();
            string strAttestation = attestationRegEx.Match(data).Groups[0].ToString();

            X509Certificate2 statement;
            X509Certificate2 attestation;

            if (!string.IsNullOrWhiteSpace(strStatement) && !string.IsNullOrWhiteSpace(strAttestation))
            {
                try
                {
                    Logger.Log("Trying to parse attestation statement certificates...");
                    statement = new(Encoding.ASCII.GetBytes(strStatement));
                    attestation = new(Encoding.ASCII.GetBytes(strAttestation));
                    CsrData = csr;
                    AttestationStatement = new AttestationStatement(statement, attestation);                    
                }
                catch (Exception ex)
                {
                    Logger.Log($"Failed to parse attestation statement certificates, exception: ", ex.Message);
                    throw;
                }
            }
            else
            {
                Logger.Log("Did not find attestation certificates in the csr.");
                CsrData = csr;
            }
        }

        public AttestationStatement? AttestationStatement { get; private set; }

        public AsymmetricAlgorithm? PublicKey { get; private set; }

        public bool? MatchesAttestation { get; private set; } = false;

        private CertificateRequest? CertificateRequest { get; set; }

        public HashAlgorithmName HashAlgorithmName { get; private set; }
        public string? Subject { get; private set; }

        public string? Sid { get; private set; }

        public string[]? San { get; private set; }

        public string? CsrData { get; private set; }

        
    }
}

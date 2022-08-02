using System;
using System.Formats.Asn1;
using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace YKEnroll.Lib;

/// <summary>
///     Various certificate related helper functions.
///     Currently mostly related to generating CSR:s
///     and relevant extensions.
/// </summary>
public static class CertHelper
{    

    /// <summary>
    ///     Returns a certificate request build from the provided
    ///     public key and subject data.
    /// </summary>
    /// <param name="publicKey">The public key to build the request for.</param>
    /// <param name="subject">Subject information, e.g. CN=User.</param>
    /// <param name="upn">(Optional) UserPrincipalName, e.g. user@domain.local.</param>
    /// <param name="sid">(Optionsl) The SID of the Active Directory user account.</param>
    /// <returns></returns>
    public static CertificateRequest CreateCertificateRequest(AsymmetricAlgorithm publicKey, string subject, string upn = "", string? sid = "")
    {        
        
        CertificateRequest request;
        switch (publicKey.SignatureAlgorithm, publicKey.KeySize)
        {
            case ("ECDsa", 256):
                request = new (subject, (ECDsa)publicKey, HashAlgorithmName.SHA256);
                break;
            case ("ECDsa", 384):
                request = new (subject, (ECDsa)publicKey, HashAlgorithmName.SHA384);
                break;
            case ("RSA", 2048):
                request = new (subject, (RSA)publicKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                break;
            default:
                throw new ArgumentOutOfRangeException($"Algorithm: {publicKey.SignatureAlgorithm}, KeySize: {publicKey.KeySize.ToString()}");
        }
               
        if (!string.IsNullOrWhiteSpace(upn))
        {           
            request.CertificateExtensions.Add(CreateUserPrincipalNameExtension(upn));
        }
        
        if (!string.IsNullOrWhiteSpace(sid))
        {
            //throw new NotImplementedException("Cannot render SID extension!");
            request.CertificateExtensions.Add(CreateSidExtension(sid));
        }

        return request;
    }

    /// <summary>
    ///     Generates an X509 extension for holding the User Principal Name.
    /// </summary>
    /// <param name="upn"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static X509Extension CreateUserPrincipalNameExtension(string upn)
    {
        if (!string.IsNullOrWhiteSpace(upn))
        {
            var sanBuilder = new SubjectAlternativeNameBuilder();
            var upnCount = 0;
            foreach (var u in upn.Split(Environment.NewLine))
            { 
                if (!string.IsNullOrWhiteSpace(u))
                {
                    sanBuilder.AddUserPrincipalName(u);
                    upnCount++;
                }
            }
            if (upnCount > 0)
                return sanBuilder.Build();            
        }
        throw new ArgumentNullException("upn", "Unable to build User principal name(s) from the supplied data");
    }

    /// <summary>
    ///     Generates the OID_NTDS_CA_SECURITY_EXT extension that holds
    ///     the SID for an Active Directory user.
    /// </summary>
    /// <param name="sid"></param>
    /// <returns></returns>
    public static X509Extension CreateSidExtension(string sid)
    {             
        // Render octet string to byte
        var octet = new byte[sid.Length + 2];
        octet[0] = 0x04; // Octet string identifier tag
        octet[1] = (byte)sid.Length; // Octet string length
        Array.Copy(Encoding.ASCII.GetBytes(sid), 0, octet, 2, sid.Length);

        // ASN structure
        var writer = new AsnWriter(AsnEncodingRules.DER);
        writer.PushSequence();
        writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 0, true));
        writer.WriteObjectIdentifier("1.3.6.1.4.1.311.25.2.1");        
        writer.PushSetOf(new Asn1Tag(TagClass.ContextSpecific, 0, true));
        writer.WriteEncodedValue(octet);
        writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 0, true));
        writer.PopSetOf(new Asn1Tag(TagClass.ContextSpecific, 0, true));
        writer.PopSequence();

        var extension = new X509Extension("1.3.6.1.4.1.311.25.2", writer.Encode(), false);
        return extension;
    }

    /// <summary>
    ///     Converts a der encoded file to pem.
    ///     There are no checks of the input data
    ///     it will just try to decode to Base64 and
    ///     return it with the provided title for
    ///     header and footer.
    /// </summary>
    /// <param name="title">The PEM header, e.g. "CERTIFICATE REQUEST" or "CERTIFICATE" </param>
    /// <param name="pemData"></param>
    /// <returns></returns>
    public static string ConvertToPem(string title, byte[] pemData)
    {
        string header = $"-----BEGIN {title.Trim().ToUpper()}-----{Environment.NewLine}";
        string data = Convert.ToBase64String(pemData, Base64FormattingOptions.InsertLineBreaks);
        string footer = $"{Environment.NewLine}-----END {title.Trim().ToUpper()}-----";
        
        return header + data + footer;
    }

    /// <summary>
    ///     Strips header and footer from a PEM file.
    /// </summary>
    /// <param name="pemData"></param>
    /// <param name="flat">Will return a "flat" file ie. without line breaks</param>
    /// <returns></returns>
    public static string StripPem(string pemData, bool flat = false)
    {
        string strippedPem = "";
        foreach (string l in pemData.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!l.StartsWith("-----"))
            {
                strippedPem = flat ? strippedPem = strippedPem + l : strippedPem = strippedPem + l + Environment.NewLine;
            }
        }        
        if(flat)
            strippedPem.TrimEnd(Environment.NewLine.ToCharArray());

        return strippedPem;
    }   

    /// <summary>
    ///     Converts a pem encoded file to der.
    ///     There is no validation of the input
    ///     data. It will just strip the header
    ///     and footer and encode the data.
    /// </summary>
    /// <param name="pemData"></param>
    /// <returns></returns>
    public static byte[] ConvertToDer(string pemData)
    {
        if(pemData.ToUpper().Contains("PRIVATE KEY"))
        {
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Inputdata could contain sensitive data!"));
        }
        string firstLine = pemData.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).First();
        string lastLine = pemData.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Last();

        if(firstLine != lastLine)
        {
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, "Inputdata headers don't match!"));
        }


        byte[] derData = new byte[] { };
        string strippedPem = "";
        foreach (string l in pemData.Split(Environment.NewLine.ToCharArray(), StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if(!l.StartsWith("-----"))
            {
                strippedPem = strippedPem + l;
            }
        }

        return Convert.FromBase64CharArray(strippedPem.ToCharArray(), 0 , strippedPem.Length);
    }
}
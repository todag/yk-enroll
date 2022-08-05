using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace YKEnroll.Lib;

public class AttestationStatement
{
    private const string yubico_piv_attestation_root_ca =
        $"-----BEGIN CERTIFICATE-----" +
        $"MIIDFzCCAf+gAwIBAgIDBAZHMA0GCSqGSIb3DQEBCwUAMCsxKTAnBgNVBAMMIFl1" +
        $"YmljbyBQSVYgUm9vdCBDQSBTZXJpYWwgMjYzNzUxMCAXDTE2MDMxNDAwMDAwMFoY" +
        $"DzIwNTIwNDE3MDAwMDAwWjArMSkwJwYDVQQDDCBZdWJpY28gUElWIFJvb3QgQ0Eg" +
        $"U2VyaWFsIDI2Mzc1MTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAMN2" +
        $"cMTNR6YCdcTFRxuPy31PabRn5m6pJ+nSE0HRWpoaM8fc8wHC+Tmb98jmNvhWNE2E" +
        $"ilU85uYKfEFP9d6Q2GmytqBnxZsAa3KqZiCCx2LwQ4iYEOb1llgotVr/whEpdVOq" +
        $"joU0P5e1j1y7OfwOvky/+AXIN/9Xp0VFlYRk2tQ9GcdYKDmqU+db9iKwpAzid4oH" +
        $"BVLIhmD3pvkWaRA2H3DA9t7H/HNq5v3OiO1jyLZeKqZoMbPObrxqDg+9fOdShzgf" +
        $"wCqgT3XVmTeiwvBSTctyi9mHQfYd2DwkaqxRnLbNVyK9zl+DzjSGp9IhVPiVtGet" +
        $"X02dxhQnGS7K6BO0Qe8CAwEAAaNCMEAwHQYDVR0OBBYEFMpfyvLEojGc6SJf8ez0" +
        $"1d8Cv4O/MA8GA1UdEwQIMAYBAf8CAQEwDgYDVR0PAQH/BAQDAgEGMA0GCSqGSIb3" +
        $"DQEBCwUAA4IBAQBc7Ih8Bc1fkC+FyN1fhjWioBCMr3vjneh7MLbA6kSoyWF70N3s" +
        $"XhbXvT4eRh0hvxqvMZNjPU/VlRn6gLVtoEikDLrYFXN6Hh6Wmyy1GTnspnOvMvz2" +
        $"lLKuym9KYdYLDgnj3BeAvzIhVzzYSeU77/Cupofj093OuAswW0jYvXsGTyix6B3d" +
        $"bW5yWvyS9zNXaqGaUmP3U9/b6DlHdDogMLu3VLpBB9bm5bjaKWWJYgWltCVgUbFq" +
        $"Fqyi4+JE014cSgR57Jcu3dZiehB6UtAPgad9L5cNvua/IWRmm+ANy3O2LH++Pyl8" +
        $"SREzU8onbBsjMg9QDiSf5oJLKvd/Ren+zGY7" +
        $"-----END CERTIFICATE-----";

    public AttestationStatement(X509Certificate2 statementCert, X509Certificate2 attestingCert)
    {
        StatementCertificate = statementCert;
        AttestationCertificate = attestingCert;
        RootCertificate = new X509Certificate2(Encoding.ASCII.GetBytes(yubico_piv_attestation_root_ca));
        Subject = StatementCertificate.Subject;
        IsValid = TryBuildChain();

        ReadExtensions();
    }

    private bool TryBuildChain()
    {            
        Chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        Chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        //chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreWrongUsage | X509VerificationFlags.IgnoreInvalidBasicConstraints;

        // Add X509VerificationFlags.IgnoreInvalidBasicConstraints flag because Yubikeys on firmware
        // 5.2.4 (and maybe older) does not seem to include a Basic Constraints extension in the 
        // attestation certificate so verification will fail without this flag set. On newer Yubikeys
        // (tested on 5.2.6) the Basic Constraints extension is included in the attestation certificate.
        // 
        // We could do a firmware version check here but for now just set the flag for all verifications.        
        Chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreInvalidBasicConstraints;

        Chain.ChainPolicy.CustomTrustStore.Add(RootCertificate);
        Chain.ChainPolicy.ExtraStore.Add(AttestationCertificate);
        return Chain.Build(StatementCertificate);
    }
    private void ReadExtensions()
    {
        foreach (X509Extension extension in StatementCertificate.Extensions)
        {
            byte[] value = extension.RawData;
            string oid = extension.Oid!.Value!;
            if (oid == "1.3.6.1.4.1.41482.3.3")
            {
                // Firmware version, encoded as 3 bytes, like: 040300 for 4.3.0
                FirmwareVersion = $"{(int)value[0]}.{(int)value[1]}.{(int)value[2]}";
            }
            else if (oid == "1.3.6.1.4.1.41482.3.7")
            {
                // Serial number of the YubiKey, encoded as an integer.
                // The value seems to be a 48bit value (6 bytes), try to convert:
                // Drop the first two values and reverse the array.
                // This seems to work, at least with my Yubikeys, there
                // is probably a better way though...
                byte[] serialByte = new byte[4];
                Array.Copy(value, 2, serialByte, 0, 4);
                Array.Reverse(serialByte);
                SerialNumber = BitConverter.ToInt32(serialByte, 0);
            }
            else if (oid == "1.3.6.1.4.1.41482.3.8")
            {
                // Two bytes, the first encoding pin policy and the second touch policy
                // Pin policy: 01 - never, 02 - once per session, 03 - always
                // Touch policy: 01 - never, 02 - always, 03 - cached for 15s
                if (value[0] == 01)
                    PinPolicy = "Never";
                if (value[0] == 02)
                    PinPolicy = "Once per session";
                if (value[0] == 03)
                    PinPolicy = "Always";

                if (value[1] == 01)
                    TouchPolicy = "Never";
                if (value[1] == 02)
                    TouchPolicy = "Once per session";
                if (value[1] == 03)
                    TouchPolicy = "Cached";
            }
            else if (oid == "1.3.6.1.4.1.41482.3.9")
            {
                // Formfactor, encoded as one byte
                // USB - A Keychain: 01(81 for FIPS Devices)
                // USB - A Nano: 02(83 for FIPS Devices)
                // USB - C Keychain: 03(84 for FIPS Devices)
                // USB - C Nano: 04(84 for FIPS Devices)
                // Lightning and USB - C: 05(85 for FIPS Devices)
                if (value[0] == 01 || value[0] == 81)
                    FormFactor = "USB - A Keychain";
                if (value[0] == 02 || value[0] == 83)
                    FormFactor = "USB - A Nano";
                if (value[0] == 03 || value[0] == 84)
                    FormFactor = "USB - C Nano";
                if (value[0] == 04 || value[0] == 84)
                    FormFactor = "USB - C Nano";
                if (value[0] == 05 || value[0] == 85)
                    FormFactor = "Lightning and USB - C";
            }
        }
    }
    public X509Certificate2 StatementCertificate { get; private set; }
    public X509Certificate2 AttestationCertificate { get; private set; }
    public X509Certificate2 RootCertificate { get; private set; }

    public X509Chain Chain { get; private set; } = new();
    public bool IsValid { get; private set; } = false;
    public string FirmwareVersion { get; private set; } = "";
    public int SerialNumber { get; private set; }
    public string PinPolicy { get; private set; } = "";
    public string TouchPolicy { get; private set; } = "";
    public string Subject { get; private set; } = "";
    public string FormFactor { get; private set; } = "";
}

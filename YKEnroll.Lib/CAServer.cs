using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CERTCLILib;

namespace YKEnroll.Lib;

public class CAServer
{
    /*
     * CERTCLILib constants
     */
    private const int CC_DEFAULTCONFIG = 0;
    private const int CC_UIPICKCONFIG = 0x1;
    private const int CR_DISP_DENIED = 2;
    private const int CR_DISP_ERROR = 1;
    private const int CR_DISP_INCOMPLETE = 0;
    private const int CR_DISP_ISSUED = 0x3;
    private const int CR_DISP_ISSUED_OUT_OF_BAND = 4;
    private const int CR_DISP_REVOKED = 6;
    private const int CR_DISP_UNDER_SUBMISSION = 0x5;
    private const int CR_IN_BASE64 = 0x1;
    private const int CR_IN_FORMATANY = 0;
    private const int CR_IN_PKCS10 = 0x100;
    private const int CR_OUT_BASE64 = 0x1;
    private const int CR_OUT_CHAIN = 0x100;
   
    public List<CertificateTemplate> CertificateTemplates { get; set; } = new();
    public string DisplayName { get; set; } = string.Empty;

    public string DnsHostName { get; set; } = string.Empty;

    public string CommonName { get; set; } = string.Empty;

    public string Config => $"{DnsHostName}\\{CommonName}";

    /// <summary>
    ///     Requests a certificate from this CA.
    /// </summary>
    /// <param name="certTemplate">Certificate Templatr to target in the request.</param>
    /// <param name="csrData">Base64 encoded CSR</param>
    /// <returns>CAResponse object.</returns>
    /// <exception cref="NotSupportedException"></exception>
    public CAResponse RequestCertificate(CertificateTemplate certTemplate, string csrData)
    {
        var caResponse = new CAResponse();

        var attr = "CertificateTemplate:" + certTemplate.Name;

        var certRequest = new CCertRequest();
        caResponse.ResponseCode = certRequest.Submit(CR_IN_FORMATANY, csrData, attr, Config);

        switch (caResponse.ResponseString)
        {
            case "CR_DISP_ISSUED":
                caResponse.Certificate =
                    new X509Certificate2(Encoding.UTF8.GetBytes(certRequest.GetCertificate(CR_OUT_BASE64)));
                break;
            case "CR_DISP_UNDER_SUBMISSION":
                caResponse.RequestId = certRequest.GetRequestId();
                caResponse.RequestIdString = certRequest.GetRequestIdString();
                break;
            case "CR_DISP_DENIED":
                break;
            default:
                throw new NotSupportedException($"Unknown or unsupported answer from CA [{caResponse.ResponseString}]");
        }

        return caResponse;
    }

    /// <summary>
    ///     Retrieves an issued certificate from this CA.
    /// </summary>
    /// <param name="requestId">Id of the request to retrieve.</param>
    /// <returns>CAResponse object.</returns>
    public CAResponse RetrieveCertificate(int requestId)
    {
        var caResponse = new CAResponse();
        var certRequest = new CCertRequest();
        caResponse.ResponseCode = certRequest.RetrievePending(requestId, Config);
        switch (caResponse.ResponseString)
        {
            case "CR_DISP_ISSUED":
                caResponse.Certificate =
                    new X509Certificate2(Encoding.UTF8.GetBytes(certRequest.GetCertificate(CR_OUT_BASE64)));
                break;
            case "CR_DISP_UNDER_SUBMISSION":
                caResponse.RequestId = certRequest.GetRequestId();
                caResponse.RequestIdString = certRequest.GetRequestIdString();
                break;
        }
        return caResponse;
    }
}
#define CERTCLILib
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
#if (CERTCLILib)
using CERTCLILib;
#endif

namespace YKEnroll.Lib;

public class MSCACertServer : ICertServer
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

    /*
     * CertReq.exe
     */
    private readonly string certReqCsrTmpFile = $"{Path.GetTempPath()}yk-enroll.tmp.csr";
    private readonly string certReqCrtTmpFile = $"{Path.GetTempPath()}yk-enroll.tmp.crt";


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
    /// <returns>CertServerResponse object.</returns>
    /// <exception cref="NotSupportedException"></exception>
    public CertServerResponse RequestCertificate(CertificateTemplate certTemplate, string csrData)
    {
#if (CERTCLILib)
        if(!Settings.UseCertReq)
            return RequestWithCERTCLILib(certTemplate, csrData);
        else
            return RequestWithCertReq(certTemplate, csrData);
#else
        return RequestWithCertReq(certTemplate, csrData);
#endif
    }

    /// <summary>
    ///     Retrieves an issued certificate from this CA.
    /// </summary>
    /// <param name="requestId">Id of the request to retrieve.</param>
    /// <returns>CertServerResponse object.</returns>
    public CertServerResponse RetrieveCertificate(string requestId)
    {
#if (CERTCLILib)
        if(!Settings.UseCertReq)
            return RetrieveWithCERTCLILib(requestId);
        else
            return RetrieveWithCertReq(requestId);
#else
        return RetrieveWithCertReq(requestId);
#endif 
    }

#if (CERTCLILib)
    private CertServerResponse RequestWithCERTCLILib(CertificateTemplate certTemplate, string csrData)
    {
        var attr = "CertificateTemplate:" + certTemplate.Name;
        var certRequest = new CCertRequest();
        int responseCode = certRequest.Submit(CR_IN_FORMATANY, csrData, attr, Config);

        switch (responseCode)
        {
            case CR_DISP_ISSUED:
                return new CertServerResponse(
                    RequestStatus.CR_ISSUED,
                    certRequest.GetRequestIdString(),
                    new X509Certificate2(Encoding.UTF8.GetBytes(certRequest.GetCertificate(CR_OUT_BASE64)))
                    );
            case CR_DISP_UNDER_SUBMISSION:
                return new CertServerResponse(
                    RequestStatus.CR_PENDING,
                    certRequest.GetRequestIdString()
                    );
            case CR_DISP_DENIED:
                return new CertServerResponse(
                    RequestStatus.CR_DENIED,
                    certRequest.GetRequestIdString()
                    );
            default:
                throw new NotSupportedException($"Unknown or unsupported answer from Cert Server [{responseCode}]");
        }
    }

    private CertServerResponse RetrieveWithCERTCLILib(string requestId)
    {
        var certRequest = new CCertRequest();
        int responseCode = certRequest.RetrievePending(Int32.Parse(requestId), Config);

        switch (responseCode)
        {
            case CR_DISP_ISSUED:
                X509Certificate2 certificate = new X509Certificate2(Encoding.UTF8.GetBytes(certRequest.GetCertificate(CR_OUT_BASE64)));
                return new CertServerResponse(
                    RequestStatus.CR_ISSUED,
                    requestId, certificate
                    );
            case CR_DISP_UNDER_SUBMISSION:
                return new CertServerResponse(
                    RequestStatus.CR_PENDING,
                    certRequest.GetRequestIdString()
                    );
            case CR_DISP_DENIED:
                return new CertServerResponse(
                    RequestStatus.CR_DENIED,
                    certRequest.GetRequestIdString()
                    );
            default:
                throw new NotSupportedException($"Unknown or unsupported answer from Cert Server [{responseCode}]");
        }
    }
#endif

    private CertServerResponse CertReq(string arguments)
    {
        Process process = new System.Diagnostics.Process();
        process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        process.StartInfo.FileName = "C:\\Windows\\System32\\certreq.exe";
        process.StartInfo.Arguments = "-q -f " + arguments;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardInput = true;
        Logger.Log($"Calling certreq.exe with arguments: \"{arguments}\"");
        process.Start();
        string output = "";
        while (!process.HasExited)
        {
            output += process.StandardOutput.ReadToEnd();
        }
        Logger.Log($"CertReq.exe exited with code: {process.ExitCode}, arguments passed: \"{process.StartInfo.Arguments}\" Output:\n{output}");
        if (process.ExitCode != 0)
        {
            throw new Exception($"certreq.exe exited with code: {process.ExitCode}\nArguments: \"{process.StartInfo.Arguments}\" Output: \n{output}");
        }

        if (output.Contains("Certificate retrieved(Issued) Issued"))
        {
            return new CertServerResponse(
                RequestStatus.CR_ISSUED,
                new X509Certificate2(certReqCrtTmpFile)
                );
        }
        else if (output.Contains("Certificate not issued (Denied)"))
            return new CertServerResponse(RequestStatus.CR_DENIED);
        else if (output.Contains("Certificate request is pending: Taken Under Submission"))
        {
            Regex requestIdRegEx = new Regex("RequestId: \"?(\\d+)\"?");
            string requestId = requestIdRegEx.Match(output).Groups[1].Value;
            return new CertServerResponse(
                RequestStatus.CR_PENDING,
                requestId
                );
        }
        else
            throw new NotSupportedException($"Unknown or unsupported answer from Cert Server\n{output}");
    }

    private CertServerResponse RequestWithCertReq(CertificateTemplate certTemplate, string csrData)
    {        
        File.WriteAllText(certReqCsrTmpFile, csrData);
        return CertReq($"-submit -config {Config} -attrib \"CertificateTemplate:{certTemplate.Name}\" {certReqCsrTmpFile} {certReqCrtTmpFile}");
    }

    private CertServerResponse RetrieveWithCertReq(string requestId)
    {
        return CertReq($"-retrieve -config {Config} {requestId} {certReqCrtTmpFile}");
    }
}
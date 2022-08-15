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
    /// <returns>CAResponse object.</returns>
    /// <exception cref="NotSupportedException"></exception>
    public CAResponse RequestCertificate(CertificateTemplate certTemplate, string csrData)
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
    /// <returns>CAResponse object.</returns>
    public CAResponse RetrieveCertificate(int requestId)
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
    private CAResponse RequestWithCERTCLILib(CertificateTemplate certTemplate, string csrData)
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

    private CAResponse RetrieveWithCERTCLILib(int requestId)
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
#endif

    private CAResponse CertReq(string arguments)
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

        CAResponse caResponse = new CAResponse();

        if (output.Contains("Certificate retrieved(Issued) Issued"))
        {
            caResponse.ResponseCode = CR_DISP_ISSUED;
            caResponse.Certificate = new X509Certificate2(certReqCrtTmpFile);
        }
        else if (output.Contains("Certificate not issued (Denied)"))
            caResponse.ResponseCode = CR_DISP_DENIED;
        else if (output.Contains("Certificate request is pending: Taken Under Submission"))
        {
            Regex requestIdRegEx = new Regex("RequestId: \"?(\\d+)\"?");
            string r = requestIdRegEx.Match(output).Groups[1].Value;
            caResponse.RequestId = int.Parse(requestIdRegEx.Match(output).Groups[1].Value);
            caResponse.ResponseCode = CR_DISP_UNDER_SUBMISSION;
        }
        else
            caResponse.ResponseCode = CR_DISP_ERROR;
        
        return caResponse;
    }

    private CAResponse RequestWithCertReq(CertificateTemplate certTemplate, string csrData)
    {        
        File.WriteAllText(certReqCsrTmpFile, csrData);
        return CertReq($"-submit -config {Config} -attrib \"CertificateTemplate:{certTemplate.Name}\" {certReqCsrTmpFile} {certReqCrtTmpFile}");
    }

    private CAResponse RetrieveWithCertReq(int requestId)
    {
        return CertReq($"-retrieve -config {Config} {requestId} {certReqCrtTmpFile}");
    }
}
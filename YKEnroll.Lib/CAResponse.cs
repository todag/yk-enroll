/*
using System.Security.Cryptography.X509Certificates;

namespace YKEnroll.Lib;

/// <summary>
///     An instance of this class is returned a response from a CA Server.
/// </summary>
public class CAResponse
{       
    /// <summary>
    ///     0: CR_DISP_INCOMPLETE
    ///     1: CR_DISP_ERROR
    ///     2: CR_DISP_DENIED
    ///     3: CR_DISP_ISSUED
    ///     4: CR_DISP_ISSUED_OUT_OF_BAND
    ///     5: CR_DISP_UNDER_SUBMISSION
    ///     6: CR_DISP_REVOKED
    /// </summary>
    /// 
    public int ResponseCode { get; set; }
    /// <summary>
    ///     If a request is pending CA Manager approval, this
    ///     will hold the request Id of the pending request.
    /// </summary>
    public int RequestId { get; set; }

    /// <summary>
    /// Response string rendered from ResponseCode.
    /// </summary>
    public string? ResponseString
    {
        get
        {
            switch (ResponseCode)
            {
                case 0:
                    return "CR_DISP_INCOMPLETE";
                case 1:
                    return "CR_DISP_ERROR";
                case 2:
                    return "CR_DISP_DENIED";
                case 3:
                    return "CR_DISP_ISSUED";
                case 4:
                    return "CR_DISP_ISSUED_OUT_OF_BAND";
                case 5:
                    return "CR_DISP_UNDER_SUBMISSION";
                case 6:
                    return "CR_DISP_REVOKED";
                default:
                    return "CR_DISP_UNKNOWN";
            }
        }
    }

    public string? RequestIdString { get; set; }

    /// <summary>
    ///     If a certificate is issued. This will hold the
    ///     issued certificate, otherwise it will be null.
    /// </summary>
    public X509Certificate2? Certificate { get; set; }
}

*/
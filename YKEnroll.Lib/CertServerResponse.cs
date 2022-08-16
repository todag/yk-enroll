using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace YKEnroll.Lib;

public enum RequestStatus
{
    CR_ISSUED,
    CR_PENDING,
    CR_DENIED,
    CR_ERROR
}
public class CertServerResponse
{
        
    public CertServerResponse(RequestStatus status, string? requestId, X509Certificate2? certificate) : this(status, requestId)
    {
        Certificate = certificate;
    }

    public CertServerResponse(RequestStatus status, string? requestId) : this(status)
    {        
        RequestId = requestId;
    }

    public CertServerResponse(RequestStatus status, X509Certificate2 certificate) : this(status)
    {
        Certificate = certificate;
    }

    public CertServerResponse(RequestStatus status)
    {
        Status = status;
    }
    
    public RequestStatus Status { get; private set; }

    public string? StatusMessage { get; private set; }

    public string? RequestId { get; private set; }
    
    public X509Certificate2? Certificate { get; private set; }
}

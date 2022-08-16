using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YKEnroll.Lib;

public interface ICertServer
{
    public CertServerResponse RequestCertificate(CertificateTemplate certificateTemplate, string csrData);

    public CertServerResponse RetrieveCertificate(string requestId);
}

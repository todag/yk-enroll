using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YKEnroll.Lib
{
    public static class YKEnrollLib
    {
#if (SUPPORTS_CERTCLILib)
        public static bool SUPPORTS_CERTLIBCli { get; } = true;
#else
        public static bool SUPPORTS_CERTLIBCli { get; } = false;
#endif
    }
}

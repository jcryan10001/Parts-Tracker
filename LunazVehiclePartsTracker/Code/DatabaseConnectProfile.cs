using System;
using System.Collections.Generic;
using System.Text;

namespace SAPB1Commons.B1Types
{
    public class B1DirectDBProfile
    {
        public SAPB1Commons.B1Types.DatabaseType DBType { get; set; }
        public string DBServerName { get; set; }
        public string DBTenantName { get; set; }
        public string DBUserName { get; set; }
        public string DBPassword { get; set; }

        public string ServiceLayerURL { get; set; }

        public string DatabaseName { get; set; }
    }
}

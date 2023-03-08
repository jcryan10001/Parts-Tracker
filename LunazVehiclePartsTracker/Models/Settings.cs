using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LunazVehiclePartsTracker.Models
{
    public partial class Settings
    {
        public class ConnectionDetails
        {
            public string DatabaseName { get; set; }
            public string DBPassword { get; set; }
            public string DBServerName { get; set; }
            public string DBType { get; set; }
            public string DBUserName { get; set; }
            public string DBTenantName { get; set; }
            public string ServiceLayerURL { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
        }

    }
}

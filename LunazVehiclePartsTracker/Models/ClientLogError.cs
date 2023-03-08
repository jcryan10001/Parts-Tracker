using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LunazVehiclePartsTracker.Models
{
    public class ClientLogError
    {
        public string message { get; set; }
        public string level { get; set; }
        public string logger { get; set; }
        public DateTimeOffset timestamp { get; set; }
        public string stacktrace { get; set; }
    }

    public class ClientLogErrorRequest
    {
        public List<ClientLogError> logs { get; set; }
    }
}

using System;

namespace SAPB1Commons.ServiceLayer
{
    // <Connection>
    public class Connection
    {
        public string SessionId { get; set; }
        public string Version { get; set; }
        public int SessionTimeout { get; set; } = 30;       //todo: this isn't set by the result of Login call
        public string ROUTEID { get; set; }
        public DateTime SessionStart { get; set; }
    }

}


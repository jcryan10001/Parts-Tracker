using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LunazVehiclePartsTracker.Code
{
    //TODO: These classes and the backround service need to be able to handle
    //all of the permutations of UDT/UDF configuration rather than the small subset
    //currently present.
    //Note: Currently we only create simple UDFs on existing tables
    public class UDFConfig
    {
        public const string db_Alpha = "db_Alpha";
        public const string db_Date = "db_Date";
        public const string db_Float = "db_Float";
        public const string db_Memo = "db_Memo";
        public const string db_Numeric = "db_Numeric";

        public const string st_Measurement = "st_Measurement";
        public const string st_Quantity = "st_Quantity";
        public const string st_Time = "st_Time";

        public string table { get; set; }
        public string field { get; set; }
        public string description { get; set; }
        public string type { get; set; }
        public int? size { get; set; }
        public string subtype { get; set; }
    }
    public class OchAppCfgSetting
    {
        public string progid { get; set; }
        public string module { get; set; }
        public string type { get; set; }
        public string description { get; set; }
        public string delimiter { get; set; }

        public string query { get; set; }
        public string configdata { get; set; }
        public string extconfigdata { get; set; }
        public string forms { get; set; }
    }
    public class UDTConfig
    {
        public string table { get; set; }
        public string description { get; set; }
        public bool autoinc { get; set; }
    }
    public class UserDBConfig
    {
        public List<UDTConfig> RequiredTables { get; set; }
        public List<UDFConfig> RequiredFields { get; set; }
        public List<OchAppCfgSetting> RequiredOchAppCfgSettings { get; set; }
    }
}

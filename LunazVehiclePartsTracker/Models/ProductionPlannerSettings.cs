using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProdPlanGanttTest5.Models
{
    public partial class Settings
    {
        public class ProductionPlannerSettings_Debug
        {
            public bool WriteFlatDataQuery { get; set; }
        }

        public class ProductionPlannerSettings
        {
            public bool EnableIAProject { get; set; }
            public bool EnableSubCon { get; set; }

            public bool EnablePrOLineIssuedTimeUDF { get; set; }
            public bool EnablePrOLineBookedQuantityUDF { get; set; }

            public ProductionPlannerSettings_Debug Debug { get; set; }
        }
    }
}

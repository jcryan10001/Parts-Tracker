using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LunazVehiclePartsTracker.Models;
using Task = System.Threading.Tasks.Task;

namespace LunazVehiclePartsTracker.Services
{
    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }
    public class DataService
    {
        private PetaPoco.IDatabaseBuildConfiguration databaseConfig;
        //private List<FlatTaskRecord> records;
        public DataService(ILogger<DataService> logger, IOptions<Settings.ConnectionDetails> config)
        {
            SAPB1Commons.B1Types.DatabaseType DBServerType;
            IsHana = config.Value.DBType.ToUpper() == "HANA";
            if (IsHana)
            {
                DBServerType = SAPB1Commons.B1Types.DatabaseType.Hana;
            }
            else
            {
                DBServerType = SAPB1Commons.B1Types.DatabaseType.MsSql;
            };

            var conf = new SAPB1Commons.B1Types.B1DirectDBProfile() { DatabaseName = config.Value.DatabaseName, DBPassword = config.Value.DBPassword, DBServerName = config.Value.DBServerName, DBType = DBServerType, DBUserName = config.Value.DBUserName, ServiceLayerURL = config.Value.ServiceLayerURL, DBTenantName = config.Value.DBTenantName };
            databaseConfig = SAPB1Commons.PetaPocoConnectionBuilder.BuildSAPBusinessOneConfigForPetaPoco(conf);
        }

        public bool IsHana { get; }


        //public async Task<List<filterDataList>> GetFilteredData(List<FlatTaskRecord> records)
        //{
        //    List<filterDataList> data = new List<filterDataList>();

        //    if (records != null)
        //    {
        //        foreach (var flatTask in records)
        //        {
        //            filterDataList item;
        //            var bp = flatTask.POCardCode;
        //            var pn = flatTask.Project;
        //            int so = flatTask.SODocNum;
        //            int po = flatTask.PODocNum;
        //            DateTime startd = flatTask.StartDate;
        //            DateTime endd = flatTask.EndDate;
        //            var BPD = flatTask.BPDescription;
        //            var POD = flatTask.PODescription;
        //            var PND = flatTask.PNDescription;
        //            var ITMD = flatTask.ItemDescription;
        //            var itm = flatTask.POItemCode;


        //            item = new filterDataList { BP = bp, PN = pn, SO = so, PO = po, startDate = startd, endDate = endd, BPDesc = BPD, PODesc=POD, PNDesc = PND,ItemDesc = ITMD, Item = itm };

        //            data.Add(item);
        //        };
        //    }

        //    return data;
        //}




    }
}

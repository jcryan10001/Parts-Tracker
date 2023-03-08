using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SAPB1Commons.ServiceLayer
{
    public static class OrderLookups
    {
        public static async Task<List<dynamic>> OrdersAsync(this Client conn, List<int> DocEntries, string select = "")
        {
            var final = new List<dynamic>();

            //The URL length could be an issue for large key lists - these keys are integers - go through 50 at a time
            for (int skip = 0; skip < DocEntries.Count; skip += 50)
            {
                var filt = DocEntries.Skip(skip).Take(50).Select(Key => "DocEntry eq " + Key.ToString());
                var range =
                    JObject.Parse(await conn.GetAsync(string.Format("Orders?$filter=" + string.Join(" or ", filt) + (string.IsNullOrWhiteSpace(select) ? "" : "&$select=" + select)), 50))["value"]
                    .ToObject<List<dynamic>>();

                final.AddRange(range);
            }

            return final;
        }
        public static async Task<List<dynamic>> Orders(this Client conn, string filter, string select = "", int maxpagesize = 5000)
        {
            var final =
                JObject.Parse(await conn.GetAsync(string.Format("Orders?" + (string.IsNullOrWhiteSpace(filter) ? "" : "$filter=" + filter) + (string.IsNullOrWhiteSpace(select) ? "" : "&$select=" + select)), maxpagesize))["value"]
                .ToObject<List<dynamic>>();

            return final;
        }
    }
    public static class ItemLookups
    {
        public static async Task<List<dynamic>> Items(this Client conn, List<string> ItemCodes, string select = "")
        {
            var final = new List<dynamic>();

            //The URL length could be an issue for large key lists - these keys are strings(50) - go through 20 at a time
            for (int skip = 0; skip < ItemCodes.Count; skip += 20)
            {

                var filt = ItemCodes.Skip(skip).Take(20).Select(Key => "ItemCode eq '" + SAPB1Commons.ServiceLayer.Utils.EscapeODataField(Key.ToString()) + "'");
                var range =
                    JObject.Parse(await conn.GetAsync(string.Format("Items?$filter=" + string.Join(" or ", filt) + (string.IsNullOrWhiteSpace(select) ? "" : "&$select=" + select)), 20))["value"]
                    .ToObject<List<dynamic>>();

                final.AddRange(range);
            }

            return final;
        }
        public static async Task<List<dynamic>> Items(this Client conn, string filter, string select = "", int maxpagesize = 5000)
        {
            var final =
                JObject.Parse(await conn.GetAsync(string.Format("Items?" + (string.IsNullOrWhiteSpace(filter) ? "" : "$filter=" + filter) + (string.IsNullOrWhiteSpace(select) ? "" : "&$select=" + select)), maxpagesize))["value"]
                .ToObject<List<dynamic>>();

            return final;
        }
    }

    public static class WarehouseLookups
    {
        public static async Task<List<dynamic>> Warehouses(this Client conn, List<string> WarehouseCodes, string select = "")
        {
            var final = new List<dynamic>();

            //The URL length could be an issue for large key lists - these keys are strings(50) - go through 20 at a time
            for (int skip = 0; skip < WarehouseCodes.Count; skip += 20)
            {
                var filt = WarehouseCodes.Skip(skip).Take(20).Select(Key => "WarehouseCode eq '" + SAPB1Commons.ServiceLayer.Utils.EscapeODataField(Key.ToString()) + "'");
                var range =
                    JObject.Parse(await conn.GetAsync(string.Format("Warehouses?$filter=" + string.Join(" or ", filt) + (string.IsNullOrWhiteSpace(select) ? "" : "&$select=" + select)), 20))["value"]
                    .ToObject<List<dynamic>>();

                final.AddRange(range);
            }

            return final;
        }
        public static async Task<List<dynamic>> Warehouses(this Client conn, string filter, string select = "", int maxpagesize = 5000)
        {
            var final =
                JObject.Parse(await conn.GetAsync(string.Format("Warehouses?" + (string.IsNullOrWhiteSpace(filter) ? "" : "$filter=" + filter) + (string.IsNullOrWhiteSpace(select) ? "" : "&$select=" + select)), maxpagesize))["value"]
                .ToObject<List<dynamic>>();

            return final;
        }
    }

    public static class SettingLookups
    {
        public static async Task<Dictionary<string, T>> AppCfg<T>(this Client conn, bool complete, params string[] settings)
        {
            var dict = new Dictionary<string, T>();

            if (settings == null || settings.Length == 0) return dict;

            var items = settings.Select(s => s.IndexOf('=') != -1 ? s.SafeSubstring(0, s.IndexOf('=')) : s);
            var filt = settings.Select(s => "U_ProgID eq '" + SAPB1Commons.ServiceLayer.Utils.EscapeODataField(s.IndexOf('=') != -1 ? s.SafeSubstring(0, s.IndexOf('=')) : s) + "'");
            var defaults = settings.ToDictionary(s => s.IndexOf('=') != -1 ? s.SafeSubstring(0, s.IndexOf('=')) : s, s => s.IndexOf('=') != -1 ? s.SafeSubstring(s.IndexOf('=') + 1) : s);

            JArray recs = (JArray)JObject.Parse(await conn.GetAsync("U_OCHAPPCFG?$filter=" + string.Join(" or ", filt)))["value"];

            var values = new Dictionary<string, JObject>();
            foreach (JObject rec in recs)
            {
                values.Add((string)rec["U_ProgID"], rec);
            }

            foreach(string key in items)
            {
                if (values.ContainsKey(key) && values[key]["U_ConfigData"] != null)
                {
                    dict.Add(key, (values[key]["U_ConfigData"]).ToObject<T>());
                } else if (defaults.ContainsKey(key)) 
                {
                    object defval = defaults[key];

                    T typeobj = defval == null ? default(T) : (T)Convert.ChangeType(defval, typeof(T));

                    dict.Add(key, typeobj);
                } else if (complete)
                {
                    //if the value does not have a default, and does not have a record in the DB, it will be absent in the result unless complete==true
                    dict.Add(key, default(T));
                }
            }

            return dict;
        }
        public static async Task<Dictionary<string, T>> AppCfg<T>(this Client conn, params string[] settings)
        {
            return await conn.AppCfg<T>(false, settings);
        }
    }
    public static class ProductionOrderLookups
    {
        public static async Task<List<dynamic>> ProductionOrdersAsync(this Client conn, List<int> DocEntries, string select = "")
        {
            var final = new List<dynamic>();

            //The URL length could be an issue for large key lists - these keys are integers - go through 50 at a time
            for (int skip = 0; skip < DocEntries.Count; skip += 50)
            {
                var filt = DocEntries.Skip(skip).Take(50).Select(Key => "AbsoluteEntry eq " + Key.ToString());
                var range =
                    JObject.Parse(await conn.GetAsync(string.Format("ProductionOrders?$filter=" + string.Join(" or ", filt) + (string.IsNullOrWhiteSpace(select) ? "" : "&$select=" + select)), 50))["value"]
                    .ToObject<List<dynamic>>();

                final.AddRange(range);
            }

            return final;
        }
        public static async Task<List<dynamic>> ProductionOrders(this Client conn, string filter, string select = "", int maxpagesize = 5000)
        {
            var final =
                JObject.Parse(await conn.GetAsync(string.Format("ProductionOrders?" + (string.IsNullOrWhiteSpace(filter) ? "" : "$filter=" + filter) + (string.IsNullOrWhiteSpace(select) ? "" : "&$select=" + select)), maxpagesize))["value"]
                .ToObject<List<dynamic>>();

            return final;
        }
    }
}

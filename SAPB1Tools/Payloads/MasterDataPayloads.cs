using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SAPB1Commons.ServiceLayer;
using Newtonsoft.Json.Linq;

namespace SAPB1Commons.Payloads
{
    public class MasterData
    {
        public static BatchInstruction AddItem(string Code, string Name, int GroupCode, string ProcurementMethodText, bool VatLiable, string FrgnName, string Codebars, bool InventoryItem, bool FrozenFor, bool ValidFor, Dictionary<string,object> UDFs = null)
        {
            var Payload = JObject.FromObject(new
            {
                ItemCode = Code,
                ItemName = Name,
                ForeignName = FrgnName,
                ItemsGroupCode = GroupCode,
                BarCode = Codebars,
                VatLiable = (VatLiable ? "tYES" : "tNO"),
                ProcurementMethod = (ProcurementMethodText.ToUpper() == "BUY" ? "bom_Buy" : "bom_Make"),
                InventoryItem = (InventoryItem ? "tYES" : "tNO"),
                Frozen = (FrozenFor ? "tYES" : "tNO"),
                Valid = (ValidFor ? "tYES" : "tNO")
            });

            if (UDFs != null)
            {

            }

            return new BatchInstruction()
            {
                objectName = "Items",
                method = "POST",
                payload = (dynamic)Payload
            };
        }

        public static BatchInstruction UnfreezeBusinessPartner(string CardCode)
        {
            var Payload = JObject.FromObject(new
            {
                Frozen = "tNO",
                Valid = "tYES"
            });

            return new BatchInstruction()
            {
                objectName = "BusinessPartners",
                objectKey = CardCode,
                method = "PATCH",
                payload = (dynamic)Payload
            };
        }

        public static BatchInstruction FreezeBusinessPartner(string CardCode)
        {
            var Payload = JObject.FromObject(new
            {
                Frozen = "tYES",
                Valid = "tNO"
            });

            return new BatchInstruction()
            {
                objectName = "BusinessPartners",
                objectKey = CardCode,
                method = "PATCH",
                payload = (dynamic)Payload
            };
        }

    }
}

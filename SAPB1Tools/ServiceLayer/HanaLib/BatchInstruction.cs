using System.Collections.Generic;
using System.Linq;

namespace SAPB1Commons.ServiceLayer
{
    public enum BatchInstructionPayloadType
    {
        Object = 0,
        String = 1
    }
    public partial class BatchInstruction
    {
        public string objectName { get; set; }
        public object objectKey { get; set; }
        //GET - retrieve a value
        //POST - create an object
        //PUT - set object properties using defaults for any not given
        //PATCH - set object properties leaving any not given unchanged
        //DELETE - selet an object
        public string method { get; set; } = "GET";
        public bool ReplaceCollectionsOnPatch { get; set; } = false;
        public dynamic payload { get; set; }

        public BatchInstructionPayloadType payloadtype { get; set; } = BatchInstructionPayloadType.Object;
        public string outputKey { get; set; }

        public SBOServiceLayerInstructionResponse Response = null;

        #region Orders
        public static BatchInstruction InsertOrder(dynamic fields)
        {
            return new BatchInstruction()
            {
                objectName = "Orders",
                method = "POST",
                payload = fields
            };
        }
        public static BatchInstruction UpdateOrder(int DocEntry, dynamic fields)
        {
            return new BatchInstruction()
            {
                objectName = "Orders",
                objectKey = DocEntry,
                method = "PATCH",
                payload = fields
            };
        }
        public static BatchInstruction DeleteOrder(int DocEntry)
        {
            return new BatchInstruction()
            {
                objectName = "Orders",
                objectKey = DocEntry,
                method = "DELETE"
            };
        }
        #endregion
        #region Production Orders
        public static BatchInstruction InsertProductionOrder(dynamic fields)
        {
            return new BatchInstruction()
            {
                objectName = "ProductionOrders",
                method = "POST",
                payload = fields
            };
        }

        public static BatchInstruction UpdateProductionOrder(int DocEntry, dynamic fields)
        {
            return new BatchInstruction()
            {
                objectName = "ProductionOrders",
                objectKey = DocEntry,
                method = "PATCH",
                payload = fields
            };
        }

        public static BatchInstruction DeleteProductionOrder(int DocEntry)
        {
            return new BatchInstruction()
            {
                objectName = "ProductionOrders",
                objectKey = DocEntry,
                method = "DELETE"
            };
        }
        #endregion
        #region UDT Data
        public static BatchInstruction InsertUDT(string Table, dynamic fields)
        {
            return new BatchInstruction()
            {
                objectName = "U_" + Table,
                method = "POST",
                payload = fields
            };
        }
        public static BatchInstruction UpdateUDT(string Table, string Code, dynamic fields)
        {
            return new BatchInstruction()
            {
                objectName = "U_" + Table,
                objectKey = Code,
                method = "PATCH",
                payload = fields
            };
        }
        public static BatchInstruction UpdateUDT(string Table, int Code, dynamic fields)
        {
            return new BatchInstruction()
            {
                objectName = "U_" + Table,
                objectKey = Code,
                method = "PATCH",
                payload = fields
            };
        }
        public static BatchInstruction DeleteUDT(string Table, string Code)
        {
            return new BatchInstruction()
            {
                objectName = "U_" + Table,
                objectKey = Code,
                method = "DELETE"
            };
        }
        public static BatchInstruction DeleteUDT(string Table, int Code)
        {
            return new BatchInstruction()
            {
                objectName = "U_" + Table,
                objectKey = Code,
                method = "DELETE"
            };
        }
        #endregion

        #region UDO Data
        public static BatchInstruction InsertUDO(string ObjectName, dynamic fields)
        {
            return new BatchInstruction()
            {
                objectName = ObjectName,
                method = "POST",
                payload = fields
            };
        }
        public static BatchInstruction UpdateUDO(string UDOObjectCode, string Code, dynamic fields)
        {
            return new BatchInstruction()
            {
                objectName = UDOObjectCode,
                objectKey = Code,
                method = "PATCH",
                payload = fields
            };
        }
        //public static BatchInstruction UpdateUDO(string ObjectName, int Code, dynamic fields)
        //{
        //    return new BatchInstruction()
        //    {
        //        objectName = ObjectName,
        //        objectKey = Code,
        //        method = "PATCH",
        //        payload = fields
        //    };
        //}
        public static BatchInstruction DeleteUDO(string ObjectName, string Code)
        {
            return new BatchInstruction()
            {
                objectName = ObjectName,
                objectKey = Code,
                method = "DELETE"
            };
        }
        //public static BatchInstruction DeleteUDO(string ObjectName, int Code)
        //{
        //    return new BatchInstruction()
        //    {
        //        objectName = ObjectName,
        //        objectKey = Code,
        //        method = "DELETE"
        //    };
        //}
        #endregion

        #region UDT Metadata
        public static BatchInstruction InsertUDTMD(string Table, string Description, string Type = "bott_NoObject")
        {
            return new BatchInstruction()
            {
                objectName = "UserTablesMD",
                method = "POST",
                payload = new { TableName = Table, TableDescription = Description, TableType = Type }
            };
        }

        public static BatchInstruction InsertUDFMD(string Table, string Field, string Description, string Type, int Size, string SubType = "st_None")
        {
            return new BatchInstruction()
            {
                objectName = "UserFieldsMD",
                method = "POST",
                payload = new { TableName = Table, Name = Field, Description = Description, Type = Type, SubType = SubType, Size = Size, EditSize = Size, }
            };
        }

        public static BatchInstruction InsertUDFMD(string Table, string Field, string Description, string Type, int Size, string SubType,List< SAPB1Commons.ServiceLayer.Models.SAPB1.ValidValueMD> ValidValues, string DefaultVal)
        {
            if (string.IsNullOrEmpty(SubType)) SubType = "st_None";

            return new BatchInstruction()
            {
                objectName = "UserFieldsMD",
                method = "POST",
                payload = new { TableName = Table, Name = Field, Description = Description, Type = Type, SubType = SubType, Size = Size, EditSize = Size, ValidValuesMD = ValidValues, DefaultValue = DefaultVal }
            };
        }

        public static BatchInstruction InsertUDFMD(dynamic rf, bool IsUDT = false)
        {
            var T = ((object)rf).GetType();

            dynamic dynpayload = new System.Dynamic.ExpandoObject();

            dynpayload.TableName = IsUDT ? $"@{rf.table}" : rf.table;
            dynpayload.Name = rf.field;
            dynpayload.Description = rf.description;
            dynpayload.Type = rf.type;
            if (T.GetProperties().Any(p => p.Name == "subtype"))
            {
                dynpayload.SubType = rf.subtype;
            }
            if (T.GetProperties().Any(p => p.Name == "size"))
            {
                dynpayload.Size = rf.size;
                dynpayload.EditSize = rf.size;
            }
            if (T.GetProperties().Any(p => p.Name == "default_value"))
            {
                dynpayload.DefaultValue = rf.default_value;
            }
            if (T.GetProperties().Any(p => p.Name == "valid_values") && rf.valid_values != null)
            {
                dynpayload.ValidValuesMD = ((IDictionary<string, string>)rf.valid_values).Select(kv => new { Value = kv.Key, Description = kv.Value });
            }

            return new BatchInstruction()
            {
                objectName = "UserFieldsMD",
                method = "POST",
                payload = dynpayload
            };
        }
        #endregion
        #region Picking Lists
        public static BatchInstruction InsertPickList(dynamic fields)
        {
            return new BatchInstruction()
            {
                objectName = "PickLists",
                method = "POST",
                payload = fields
            };
        }
        #endregion

        #region Partners
        public static BatchInstruction UpdatePartner(string Name, dynamic fields)
        {
            return new BatchInstruction()
            {
                objectName = "PartnersSetups",
                objectKey = Name,
                method = "PATCH",
                payload = fields
            };
        }

        #endregion

    }

}


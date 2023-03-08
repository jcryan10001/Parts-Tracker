using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SAPB1Commons.ServiceLayer;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SAPB1Commons.ServiceLayer
{
    public class UDOPayloads
    {
        public static BatchInstruction AddUDOInstruction(SAPUDORecord record, List<string> ExtraPropertiesToRemove = null)
        {
            var Payload = JObject.FromObject(record, new Newtonsoft.Json.JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
            SLHelpers.ConvertPayloadPropertiesToSLFormat(record, Payload);
            SLHelpers.RemoveUnwantedUDOPayloadProperties(Payload, false, ExtraPropertiesToRemove);

            //add to the payload any UDFs which are not part of the POCO
            SLHelpers.AddUDFsToPayload(record.UserFields, Payload);

            //POST /b1s/v1/UDO_MASTER where "UDO_MASTER" is record.Object
            return BatchInstruction.InsertUDO(record.UDOObjectCode, (dynamic)Payload);
        }

        public static BatchInstruction AddUDOInstruction<T>(SAPUDORecord record, List<string> ExtraPropertiesToRemove = null, List<T> ChildRecords = null) where T : SAPUDOChildRecord
        {
            var Payload = JObject.FromObject(record, new Newtonsoft.Json.JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
            SLHelpers.ConvertPayloadPropertiesToSLFormat(record, Payload);
            SLHelpers.RemoveUnwantedUDOPayloadProperties(Payload, false, ExtraPropertiesToRemove);

            //add to the payload any UDFs which are not part of the POCO
            SLHelpers.AddUDFsToPayload(record.UserFields, Payload);

            if (ChildRecords != null)
            {
                if (ChildRecords.Count() > 0)
                {
                    JArray jarrayObj = new JArray();

                    foreach (var child in ChildRecords)
                    {
                        var ChildPayload = JObject.FromObject(child, new Newtonsoft.Json.JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                        SLHelpers.ConvertPayloadPropertiesToSLFormat(child, ChildPayload);
                        SLHelpers.RemoveUnwantedUDOPayloadProperties(ChildPayload, true, new List<string>() { "LineId" });

                        //add to the payload any UDFs which are not part of the POCO
                        SLHelpers.AddUDFsToPayload(child.UserFields, ChildPayload);

                        jarrayObj.Add(ChildPayload);
                    }

                    Payload[ChildRecords.First().TableName.Substring(1) + "Collection"] = jarrayObj;
                }
            }

            //POST /b1s/v1/UDO_MASTER where "UDO_MASTER" is record.Object
            return BatchInstruction.InsertUDO(record.UDOObjectCode, (dynamic)Payload);
        }


        public static BatchInstruction UpdateUDOInstruction(SAPUDORecord record, List<string> ExtraPropertiesToRemove = null, List<string> ExtraPropertiesToNULL = null)
        {
            var Payload = (dynamic)JObject.FromObject(record);
            SLHelpers.ConvertPayloadPropertiesToSLFormat(record, Payload);
            SLHelpers.RemoveUnwantedUDOPayloadProperties(Payload, true, ExtraPropertiesToRemove);

            //add to the payload any UDFs which are not part of the POCO
            SLHelpers.AddUDFsToPayload(record.UserFields, Payload);

            //PATCH /b1s/v1/UDO_MASTER where "UDO_MASTER" is record.Object
            return BatchInstruction.UpdateUDO(record.UDOObjectCode, record.Code, Payload);
        }

        public static BatchInstruction UpdateUDOInstruction<T>(SAPUDORecord record, List<T> ChildRecords) where T : SAPUDOChildRecord
        {
            var Payload = (dynamic)JObject.FromObject(record);
            SLHelpers.ConvertPayloadPropertiesToSLFormat(record, Payload);
            SLHelpers.RemoveUnwantedUDOPayloadProperties(Payload, true);

            //add to the payload any UDFs which are not part of the POCO
            SLHelpers.AddUDFsToPayload(record.UserFields, Payload);

            if (ChildRecords.Count() > 0)
            {
                JArray jarrayObj = new JArray();

                foreach (var child in ChildRecords)
                {
                    var ChildPayload = JObject.FromObject(child);
                    SLHelpers.ConvertPayloadPropertiesToSLFormat(child, ChildPayload);
                    SLHelpers.RemoveUnwantedUDOPayloadProperties(ChildPayload, true);

                    //add to the payload any UDFs which are not part of the POCO
                    SLHelpers.AddUDFsToPayload(child.UserFields, ChildPayload);

                    jarrayObj.Add(ChildPayload);
                }

                Payload[ChildRecords.First().TableName.Substring(1) + "Collection"] = jarrayObj;
            }

            //PATCH /b1s/v1/UDO_MASTER where "UDO_MASTER" is record.Object
            return BatchInstruction.UpdateUDO(record.UDOObjectCode, record.Code, Payload);
        }

        public static BatchInstruction UpdateUDOInstruction<T, T2>(SAPUDORecord record, List<T> ChildRecords, List<T2> ChildRecords2) where T : SAPUDOChildRecord where T2 : SAPUDOChildRecord
        {
            var Payload = (dynamic)JObject.FromObject(record);
            SLHelpers.ConvertPayloadPropertiesToSLFormat(record, Payload);
            SLHelpers.RemoveUnwantedUDOPayloadProperties(Payload, true);

            //add to the payload any UDFs which are not part of the POCO
            SLHelpers.AddUDFsToPayload(record.UserFields, Payload);

            if (ChildRecords.Count() > 0)
            {
                JArray jarrayObj = new JArray();

                foreach (var child in ChildRecords)
                {
                    var ChildPayload = JObject.FromObject(child);
                    SLHelpers.ConvertPayloadPropertiesToSLFormat(child, ChildPayload);
                    SLHelpers.RemoveUnwantedUDOPayloadProperties(ChildPayload, true);

                    //add to the payload any UDFs which are not part of the POCO
                    SLHelpers.AddUDFsToPayload(child.UserFields, ChildPayload);

                    jarrayObj.Add(ChildPayload);
                }

                Payload[ChildRecords.First().TableName.Substring(1) + "Collection"] = jarrayObj;
            }

            if (ChildRecords2.Count() > 0)
            {
                JArray jarrayObj = new JArray();

                foreach (var child in ChildRecords2)
                {
                    var ChildPayload = JObject.FromObject(child);
                    SLHelpers.ConvertPayloadPropertiesToSLFormat(child, ChildPayload);
                    SLHelpers.RemoveUnwantedUDOPayloadProperties(ChildPayload, true);

                    //add to the payload any UDFs which are not part of the POCO
                    SLHelpers.AddUDFsToPayload(child.UserFields, ChildPayload);

                    jarrayObj.Add(ChildPayload);
                }

                Payload[ChildRecords2.First().TableName.Substring(1) + "Collection"] = jarrayObj;
            }

            //PATCH /b1s/v1/UDO_MASTER where "UDO_MASTER" is record.Object
            return BatchInstruction.UpdateUDO(record.UDOObjectCode, record.Code, Payload);
        }


        /// <summary>
        /// Creates a service layer instruction to add a single child UDO record to a parent. 
        /// </summary>
        /// <param name="parent">The only property of the parent record that is added to the payload is the "Code" so a skeleton parent can be passed.</param>
        /// <param name="child"></param>
        /// <returns></returns>
        public static BatchInstruction AddUDOChildInstruction(SAPUDORecord parent, SAPUDOChildRecord child)
        {
            var Payload = JObject.FromObject(new { Code = parent.Code });

            var ChildPayload = JObject.FromObject(child, new Newtonsoft.Json.JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
            SLHelpers.ConvertPayloadPropertiesToSLFormat(child, ChildPayload);
            SLHelpers.RemoveUnwantedUDOPayloadProperties(ChildPayload, true);

            //add to the payload any UDFs which are not part of the POCO
            SLHelpers.AddUDFsToPayload(child.UserFields, ChildPayload);

            JArray jarrayObj = new JArray();
            jarrayObj.Add(ChildPayload);
            Payload[child.TableName.Substring(1) + "Collection"] = jarrayObj;

            //POST /b1s/v1/UDO_MASTER where "UDO_MASTER" is record.Object
            return BatchInstruction.UpdateUDO(parent.UDOObjectCode, parent.Code, (dynamic)Payload);
        }

        /// <summary>
        /// Creates a service layer instruction to add one or more child UDO records to a parent. 
        /// </summary>
        /// <param name="parent">The only property of the parent record that is added to the payload is the "Code" so a skeleton parent can be passed.</param>
        /// <param name="children">All child records must be of the same type.</param>
        /// <returns></returns>
        public static BatchInstruction AddUDOChildInstruction(SAPUDORecord parent, List<SAPUDOChildRecord> children)
        {
            var Payload = JObject.FromObject(new { Code = parent.Code });
            JArray jarrayObj = new JArray();

            foreach (var child in children)
            {
                var ChildPayload = JObject.FromObject(child, new Newtonsoft.Json.JsonSerializer { NullValueHandling = NullValueHandling.Ignore });
                SLHelpers.ConvertPayloadPropertiesToSLFormat(child, ChildPayload);

                //todo: should the "LineId" property be being removed here?

                SLHelpers.RemoveUnwantedUDOPayloadProperties(ChildPayload, true, new List<string>() { "LineId" });

                //add to the payload any UDFs which are not part of the POCO
                SLHelpers.AddUDFsToPayload(child.UserFields, ChildPayload);

                jarrayObj.Add(ChildPayload);
            }

            Payload[children.First().TableName.Substring(1) + "Collection"] = jarrayObj;

            //POST /b1s/v1/UDO_MASTER where "UDO_MASTER" is record.Object
            return BatchInstruction.UpdateUDO(parent.UDOObjectCode, parent.Code, (dynamic)Payload);
        }

        public static BatchInstruction UpdateUDOChildrenInstruction(SAPUDORecord parent, List<SAPUDOChildRecord> children, List<string> ExtraPropertiesToRemove = null)
        {
            return UpdateUDOChildrenInstruction(parent.UDOObjectCode, parent.Code, children, ExtraPropertiesToRemove);
        }

        public static BatchInstruction UpdateUDOChildrenInstruction(string UDOObjectCode, string ParentCode, List<SAPUDOChildRecord> children, List<string> ExtraPropertiesToRemove = null)
        {
            var Payload = JObject.FromObject(new { Code = ParentCode });

            if (children != null)
            {
                JArray jarrayObj = new JArray();

                foreach (var child in children)
                {
                    var ChildPayload = JObject.FromObject(child);
                    SLHelpers.ConvertPayloadPropertiesToSLFormat(child, ChildPayload);
                    SLHelpers.RemoveUnwantedUDOPayloadProperties(ChildPayload, true, ExtraPropertiesToRemove);

                    //add to the payload any UDFs which are not part of the POCO
                    SLHelpers.AddUDFsToPayload(child.UserFields, ChildPayload);

                    jarrayObj.Add(ChildPayload);
                }

                Payload[children.First().TableName.Substring(1) + "Collection"] = jarrayObj;
            }

            SLHelpers.RemoveUnwantedUDOPayloadProperties(Payload, true);

            //PATCH /b1s/v1/UDO_MASTER where "UDO_MASTER" is record.Object
            return BatchInstruction.UpdateUDO(UDOObjectCode, ParentCode, Payload);
        }

        public static BatchInstruction RemoveUDOChildInstruction(SAPUDORecord parent, SAPUDOChildRecord child)
        {
            var Payload = JObject.FromObject(new { Code = parent.Code });

            JArray jarrayObj = new JArray();

            using (var dataContext = new PetaPoco.Database(SAPB1Commons.Globals.SAPBusinessOneConfigForPetaPoco))
            {
                var SQL = new PetaPoco.Sql();
                SQL.Append("Select \"LineId\" from");
                SQL.Append("\"" + child.TableName.Replace("@", "@@") + "\"");
                SQL.Append(" Where \"Code\" = @0", parent.Code);
                SQL.Append(" and \"LineId\" <> @0", child.LineId);
                SQL.Append(" order by \"LineId\"");

                var children = dataContext.Fetch<int>(SQL);    //this is the line id of every line we don't want to delete

                if (children.Count > 0)
                {

                    foreach (var dbchild in children)
                    {
                        var LinePayload = JObject.FromObject(new { LineId = dbchild });
                        jarrayObj.Add(LinePayload);
                    }

                }

                Payload[child.TableName.Substring(1) + "Collection"] = jarrayObj;
            }

            SLHelpers.RemoveUnwantedUDOPayloadProperties(Payload, true);

            //PATCH /b1s/v1/UDO_MASTER where "UDO_MASTER" is record.Object
            BatchInstruction SLIntruction = BatchInstruction.UpdateUDO(parent.UDOObjectCode, parent.Code, Payload);
            SLIntruction.ReplaceCollectionsOnPatch = true;

            return SLIntruction;
        }

        public static BatchInstruction RemoveUDOChildrenInstruction(SAPUDORecord parent, string ChildUDOObjectName, List<int> LineIdsToRetain)
        {
            var Payload = JObject.FromObject(new { Code = parent.Code });

            JArray jarrayObj = new JArray();

            foreach (var ChildLineId in LineIdsToRetain)
            {
                var LinePayload = JObject.FromObject(new { LineId = ChildLineId });
                jarrayObj.Add(LinePayload);
            }

            Payload[ChildUDOObjectName + "Collection"] = jarrayObj;

            SLHelpers.RemoveUnwantedUDOPayloadProperties(Payload, true);

            //PATCH /b1s/v1/UDO_MASTER where "UDO_MASTER" is record.Object
            BatchInstruction SLIntruction = BatchInstruction.UpdateUDO(parent.UDOObjectCode, parent.Code, Payload);
            SLIntruction.ReplaceCollectionsOnPatch = true;

            return SLIntruction;
        }
    }
}

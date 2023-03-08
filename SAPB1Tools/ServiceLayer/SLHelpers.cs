using Newtonsoft.Json.Linq;
using SAPB1Commons.ServiceLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using SAPB1Commons.B1Types;
using PetaPoco.Custom.Mappers;

namespace SAPB1Commons.ServiceLayer
{
    public class SLHelpers
    {
        public static void RemoveUnwantedSAPExtendableTabledProperties(JObject Payload)
        {
            Payload.Remove("TableName");
            Payload.Remove("FixedUserFields");
            Payload.Remove("UserFields");
        }

        public static void RemoveUnwantedUDOPayloadProperties(JObject Payload, bool RemoveCodeProperty, List<string>ExtraPropertiesToRemove = null)
        {
            Payload.Remove("UDOObjectCode");
            Payload.Remove("CreateDate");
            Payload.Remove("CreateTime");
            Payload.Remove("UpdateDate");
            Payload.Remove("UpdateTime");
            Payload.Remove("UserSign");
            Payload.Remove("TableName");
            Payload.Remove("FixedUserFields");
            Payload.Remove("UserFields");
            Payload.Remove("DocEntry");

            if (RemoveCodeProperty)
            {
                Payload.Remove("Code");
            }

            if (ExtraPropertiesToRemove != null)
            {
                foreach (string s in ExtraPropertiesToRemove)
                {
                    Payload.Remove(s);
                }

            }
        }

        public static void RemoveUnwantedChildUDOPayloadProperties(JObject Payload, List<string> childElementsToRemove, string nameOfChildCollectionTag)
        {
            //Loop through all of the child elements and remove any properties that SAP should handle upon the create event.
            foreach (JObject child in Payload[nameOfChildCollectionTag])
            {
                foreach (string field in childElementsToRemove)
                {
                    child.Remove(field);
                }
            }
        }

        public static void ConvertPayloadPropertiesToSLFormat(object Record, JObject Payload)
        {
            var SourceRecordType = Record.GetType();
            var NewProps = new List<JProperty>();
            var PropsToRemove = new List<JProperty>();

            foreach (var prop in Payload.Properties())
            {
                var sourceProperty = SourceRecordType.GetProperty(prop.Name);

                if (Attribute.IsDefined(sourceProperty, typeof(MapYNStringAsBooleanAttribute)))
                {
                    prop.Value = ((bool?)prop.Value).GetValueOrDefault() ? MapYNStringAsBooleanAttribute.TrueValue : MapYNStringAsBooleanAttribute.FalseValue;
                }

                if (Attribute.IsDefined(sourceProperty, typeof(MapYesNoStringAsBooleanAttribute)))
                {
                    prop.Value = ((bool?)prop.Value).GetValueOrDefault() ? MapYesNoStringAsBooleanAttribute.TrueValue : MapYesNoStringAsBooleanAttribute.FalseValue;
                }

                if (Attribute.IsDefined(sourceProperty, typeof(ServiceLayerFieldName)))
                {
                    var Attr = sourceProperty.GetCustomAttributes(typeof(ServiceLayerFieldName), true).First() as ServiceLayerFieldName;
                    var Replacement = new JProperty(Attr.SLName, prop.Value);
                    NewProps.Add(Replacement);
                    PropsToRemove.Add(prop);
                }

                if (Attribute.IsDefined(sourceProperty, typeof(ServiceLayerIgnore)))
                {
                    PropsToRemove.Add(prop);
                }
                else if (Attribute.IsDefined(sourceProperty, typeof(ServiceLayerReadOnly)))
                {
                    PropsToRemove.Add(prop);
                }
            }

            foreach (var prop in PropsToRemove)
            {
                Payload.Remove(prop.Name);
            }

            foreach (var prop in NewProps)
            {
                Payload.Add(prop);
            }
        }

        public static void AddUDFsToPayload(List<SAPUserField> UDFs, JProperty Sibling)
        {
            foreach (var UDF in UDFs)
            {
                AddPropertyToPayload(UDF, Sibling);
            }
        }

        public static void AddUDFsToPayload(List<SAPUserField> UDFs, JObject Payload)
        {
            foreach (var UDF in UDFs)
            {
                AddPropertyToPayload(UDF, Payload);
            }
        }

        public static void AddPropertyToPayload(SAPUserField UDF, JProperty Sibling)
        {
            if (UDF.Value == null)
            {
                if (UDF.Definition.NotNull == "Y")
                {
                    if (UDF.Definition.TypeID == "N" || UDF.Definition.TypeID == "B")
                    {
                        Sibling.AddAfterSelf(new JProperty("U_" + UDF.Definition.AliasID, 0));
                    }
                    else
                    {
                        Sibling.AddAfterSelf(new JProperty("U_" + UDF.Definition.AliasID, ""));
                    }
                }
                else
                {
                    Sibling.AddAfterSelf(new JProperty("U_" + UDF.Definition.AliasID, null));
                }
            }
            else
            {
                Sibling.AddAfterSelf(new JProperty("U_" + UDF.Definition.AliasID, UDF.Value));
            }
        }

        public static void AddPropertyToPayload(SAPUserField UDF, JObject Payload)
        {
            if (UDF.Value == null)
            {
                if (UDF.Definition.NotNull == "Y")
                {
                    if (UDF.Definition.TypeID == "N" || UDF.Definition.TypeID == "B")
                    {
                        Payload["U_" + UDF.Definition.AliasID] = 0;
                    }
                    else
                    {
                        Payload["U_" + UDF.Definition.AliasID] = "";
                    }
                }
                else
                {
                    Payload["U_" + UDF.Definition.AliasID] = null;
                }
            }
            else
            {
                Payload["U_" + UDF.Definition.AliasID] = JToken.FromObject(UDF.Value);
            }

        }

        public static void AddPropertyToPayload(string UDFName, object UDFValue, JObject Payload)
        {
            if (UDFValue == null)
            {
                //we'll have to assume the field is nullable
                Payload[UDFName] = null;
            }
            else
            {
                Payload[UDFName] = JToken.FromObject(UDFValue);
            }
        }
    }
}

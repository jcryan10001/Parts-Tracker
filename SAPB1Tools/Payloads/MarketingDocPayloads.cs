using Newtonsoft.Json.Linq;
using SAPB1Commons.ServiceLayer;
using System;
using System.Collections.Generic;

namespace SAPB1Commons.Payloads
{
    public enum DocType
    {
        PurchaseInvoice = 18,
        PurchaseCreditNote = 19,
        PurchaseOrder = 22,
        SalesOrder = 17,
        SalesInvoice = 13,
        SalesCreditNote = 14,
        SalesQuote = 23,
        SalesDeliveryNote = 15,
        SalesDownPayment = 203
    }

    public class CompactMarketingDocumentLine
    {
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public string VatGroup { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public Dictionary<string, object> AdditionalFieldsAndUDFs = null;
    }

    public class MarketingDocs
    {
        public static BatchInstruction AddMarketingDocumentCompact(DocType MarketingDocType, string CardCode, DateTime DocDate, DateTime DocDueDate, string Currency, string NumAtCard, string Comments, List<CompactMarketingDocumentLine> Lines, Dictionary<string, object> AdditionalFieldsAndUDFs = null, List<String> HeadPropertiesToIgnore = null, List<string> LinePropertiesToIgnore = null)
        {
            string SLObjName = "";

            switch (MarketingDocType)
            {
                case DocType.PurchaseInvoice:
                    SLObjName = "PurchaseInvoices";
                    break;

                case DocType.PurchaseCreditNote:
                    SLObjName = "PurchaseCreditNotes";
                    break;

                case DocType.SalesQuote:
                    SLObjName = "Quotations";
                    break;

                case DocType.SalesOrder:
                    SLObjName = "Orders";
                    break;

                default:
                    throw new Exception("Unsupported MarketingDocType: " + MarketingDocType.ToString());
            }

            var Payload = JObject.FromObject(new
            {
                CardCode = CardCode,
                DocDate = DocDate.Date.ToString("yyyy-MM-dd"),
                DocDueDate = DocDueDate.Date.ToString("yyyy-MM-dd"),
                Comments = Comments,
                DocCurrency = Currency,
                NumAtCard = NumAtCard
            });

            if (HeadPropertiesToIgnore != null)
            {
                foreach (string propname in HeadPropertiesToIgnore)
                {
                    Payload.Remove(propname);
                }
            }

            if (AdditionalFieldsAndUDFs != null)
            {
                foreach (var UDF in AdditionalFieldsAndUDFs)
                {
                    SLHelpers.AddPropertyToPayload(UDF.Key, UDF.Value, Payload);
                }
            }

            var DocLines = new JArray();

            foreach (var Line in Lines)
            {
                var PayloadLine = JObject.FromObject(Line);

                if (Line.AdditionalFieldsAndUDFs != null)
                {
                    foreach (var UDF in Line.AdditionalFieldsAndUDFs)
                    {
                        SLHelpers.AddPropertyToPayload(UDF.Key, UDF.Value, PayloadLine);
                    }
                }

                if (LinePropertiesToIgnore != null)
                {
                    foreach (string propname in LinePropertiesToIgnore)
                    {
                        PayloadLine.Remove(propname);
                    }
                }

                DocLines.Add(PayloadLine);
            }

            Payload["DocumentLines"] = DocLines;

            return new BatchInstruction()
            {
                objectName = SLObjName,
                objectKey = null,
                method = "POST",
                payload = (dynamic)Payload
            };
        }

        public class DocumentLineNumAndUDFsOnly
        {
            public int LineNum { get; set; }

            [Newtonsoft.Json.JsonIgnore]
            public Dictionary<string, object> AdditionalFieldsAndUDFs = null;
        }

        public static BatchInstruction UpdateDocumentLineRoyaltyValues(DocType MarketingDocType, long DocEntry, List<DocumentLineNumAndUDFsOnly> Lines, Dictionary<string, object> AdditionalFieldsAndUDFs = null)
        {
            if (Lines.Count == 0)
            {
                throw new ArgumentException("Lines collection contains no elements.");
            }

            string SLObjName = "";

            switch (MarketingDocType)
            {
                case DocType.SalesInvoice:
                    SLObjName = "Invoices";
                    break;

                case DocType.SalesCreditNote:
                    SLObjName = "CreditNotes";
                    break;

                default:
                    throw new Exception("Unsupported MarketingDocType: " + MarketingDocType.ToString());
            }

            var Payload = JObject.FromObject(new
            {
            });

            if (AdditionalFieldsAndUDFs != null)
            {
                foreach (var UDF in AdditionalFieldsAndUDFs)
                {
                    SLHelpers.AddPropertyToPayload(UDF.Key, UDF.Value, Payload);
                }
            }

            var DocLines = new JArray();

            foreach (var Line in Lines)
            {
                var PayloadLine = JObject.FromObject(Line);

                if (Line.AdditionalFieldsAndUDFs != null)
                {
                    foreach (var UDF in Line.AdditionalFieldsAndUDFs)
                    {
                        SLHelpers.AddPropertyToPayload(UDF.Key, UDF.Value, PayloadLine);
                    }
                }

                DocLines.Add(PayloadLine);
            }

            Payload["DocumentLines"] = DocLines;

            return new BatchInstruction()
            {
                objectName = SLObjName,
                objectKey = DocEntry,
                method = "PATCH",
                payload = (dynamic)Payload
            };
        }
    }
}
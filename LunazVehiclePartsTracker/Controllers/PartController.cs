using LunazVehiclePartsTracker.Data;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Text.Json.Nodes;
using Syncfusion.Blazor.RichTextEditor;
using SL = SAPB1Commons.ServiceLayer;
using Microsoft.Extensions.Options;
using SAPB1Commons.ServiceLayer;
using Microsoft.AspNetCore.Authentication;
using System.Net;
using Microsoft.Extensions.Logging;
using System.Data;
using Org.BouncyCastle.Asn1.Pkcs;
using Syncfusion.Blazor.RichTextEditor.Internal;
using System.Threading;
using LunazVehiclePartsTracker.Code;
using Blazor3D.Lights;
using Syncfusion.Blazor.PivotView;
using System.Dynamic;
using Microsoft.AspNetCore.Authorization;
using Syncfusion.Blazor.Diagram;
using Syncfusion.Blazor.Charts.RangeNavigator.Internal;
using PetaPoco.Core;
using System.Runtime.CompilerServices;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace LunazVehiclePartsTracker.Controllers
{
    [Produces("application/json")]
    [Route("api")]
    public class PartController : ControllerBase
    {
        private IOptions<ConnectionDetails> _ConnectionDetails;
        private ConnectionPool _CM;
        PetaPoco.IDatabaseBuildConfiguration databaseConfig;
        private readonly ILogger<PartController> _logger;
        public SAPB1Commons.B1Types.DatabaseType DBServerType;
        public SL.Client company = null;
        private List<UDT> UDTs = new List<UDT>();
        private List<UDF> UDFs = new List<UDF>();
        private bool allTablesDone = false;

            



        public SL.Client GetSLClient()
        {

            DBServerType = SAPB1Commons.B1Types.DatabaseType.MsSql;


            var conf = new SAPB1Commons.B1Types.B1DirectDBProfile() { DatabaseName = "HW_TEST_NEW", DBPassword = "0ch1ba2021!", DBServerName = "OBSLDEV-1", DBType = DBServerType, DBUserName = "sa", ServiceLayerURL = "https://OBSLDEV-1:50000/b1s/v1/", DBTenantName = "" };
            databaseConfig = SAPB1Commons.PetaPocoConnectionBuilder.BuildSAPBusinessOneConfigForPetaPoco(conf);

            _logger.LogInformation("Creating Service Layer Client");
            var CM = _CM;
            var config = conf;
            var url = "https://localhost:50000/b1s/v1/";
            var companyDb = "HW_TEST_NEW";
            return CM.GetConnection(url, companyDb, "manager", "H4rt3W", false);
        }

        public PartController(ILogger<PartController> logger, ConnectionPool CM)
        {
            _logger = logger;
            _CM = CM;

        }

        //public string file = "C:/Users/jai.choudhary/source/repos/LunazVehiclePartsTracker/LunazVehiclePartsTracker/Data/VehiclePartsTracker_ModelData.json";
        public static string workingDirectory = Environment.CurrentDirectory;
        public string mainfile = workingDirectory + "\\Data\\VehiclePartsTracker_ModelData.json";
    

        //this is the main flat data query that will house the main data load from the server/database 
        // currently we are reading json data from a file which could just be plugged in to the database but will work the exact same

        [HttpGet("/api/Parts/FlatSql")]
        public async Task<Rootobject> FlatSql( string prj)
        {
            await StartAsync();
            var company = GetSLClient();
            List<Parts> parts = new List<Parts>();
            //==================================================================================================================
            try
            {
                var ins = new BatchInstruction();
                ins.method = "GET";
                ins.objectName = $"U_OCVTPARTS";
                var result1 = await company.executeSingleAsync(ins, false);
                var parsedResult = JObject.Parse(result1)["value"];
                //var toBeDeserialized = ((JArray)parsedResult["value"]);
                foreach (JToken token in parsedResult)
                {
                    var jj = JsonConvert.DeserializeObject<Parts>(token.ToString());
                    parts.Add(jj);

                }



            }
            catch (WebException wex)
            {
                if (wex.Status == WebExceptionStatus.ProtocolError)
                {
                    var resp = wex.Response as HttpWebResponse;
                    if (resp == null || resp.StatusCode != HttpStatusCode.NotFound)
                    {//we will do something here
                    }
                }
            }
            //=================================================================================================================
            
                //List<LoadedProject> loadedprj = new List<LoadedProject> { };
                var basefile = System.IO.File.ReadAllText(mainfile);
                var rootobject = new Rootobject();
                var result = JsonConvert.DeserializeObject<Rootobject>(basefile);
                List<Projects> loadedprj = result.LoadedProjects[0].Projects;
                bool contains = loadedprj.Any(p => p.ID == prj);
                if (contains == true)
                {
                result.LoadedProjects[0].Parts = parts;
                return result;
                }

                else
                {
                    //Console.WriteLine(jj);
                    result = rootobject;
                    return result;
                }
            

            }

        [HttpPost("/api/Parts/update")]
        public async Task update([FromBody] PartsUpdateResult updateList)
        {
            Client client;
            List<TreatedParts> treatedPartsListToBeDeleted = new List<TreatedParts>();
            if (company != null)
            {
                client = company;
            }
            else
            {
                client = GetSLClient();
            }
            //=====================================we get all the related treated orders and pipe them in the to-be-deleted treated records(if any)
            foreach (Parts part in updateList.DeletedItems)
            {
                try
                {
                    var ins = new BatchInstruction();
                    ins.method = "GET";
                    ins.objectName = $"U_OCHVTPARTTREATMENTS?$filter=U_PartID eq '{part.PartNo}'";
                    var result = await client.executeSingleAsync(ins, false);
                    var parsedResult = JObject.Parse(result)["value"];
                    //var toBeDeserialized = ((JArray)parsedResult["value"]);
                    foreach(JToken token in parsedResult)
                    {
                        var jj = JsonConvert.DeserializeObject<TreatedParts>(token.ToString());
                        updateList.TreatedDeletedItems.Add(jj);
                    }



                }
                catch (WebException wex)
                {
                    if (wex.Status == WebExceptionStatus.ProtocolError)
                    {
                        var resp = wex.Response as HttpWebResponse;
                        if (resp == null || resp.StatusCode != HttpStatusCode.NotFound)
                        {//we will do something here
                        }
                    }
                }
            }
            //add the treated to be deleted list 
            foreach (TreatedParts tpart in treatedPartsListToBeDeleted) 
            {
                if (!updateList.TreatedDeletedItems.Any(x => x.Code == tpart.Code))
                {
                    updateList.TreatedDeletedItems.Add(tpart);
                }
            }
            //===================================================================================
            //to add records one by one
            if (updateList.AddedItems.Count > 0)
            {
                foreach (Parts part in updateList.AddedItems)
                {
                    try
                    {
                        dynamic fields = new ExpandoObject();
                        string table = "U_OCVTPARTS";
                        fields.Name = part.PartNo;
                        fields.U_SectionID = part.SectionID;
                        fields.U_AssyRefID = part.AssyRef;
                        fields.U_PartNo = part.PartNo;
                        fields.U_Description = part.Description;
                        //fields.U_Quantity = part.Quantity;
                        fields.U_ConditionID = part.Condition;
                        fields.U_Fixing = part.Fixing;
                        fields.U_DryFit = part.DryFit;
                        fields.U_Weight = part.Weight;
                        fields.U_StorageBoxID = part.StorageBox;

                        BatchInstruction batchInstruction = new BatchInstruction();
                        batchInstruction.method = "POST";
                        batchInstruction.payload = fields;
                        batchInstruction.objectName = table;
                        var result = await client.executeSingleAsync(batchInstruction, false);



                    }
                    catch (WebException wex)
                    {
                        if (wex.Status == WebExceptionStatus.ProtocolError)
                        {
                            var resp = wex.Response as HttpWebResponse;
                            if (resp == null || resp.StatusCode != HttpStatusCode.NotFound)
                            {
                                //Service layer can be weird when creating table - slow down, give it some time
                                await Task.Delay(1000);
                                continue;
                            }
                        }
                    }

                }
            }
            //to delete records one by one
            if (updateList.DeletedItems.Count > 0)
            {
                foreach (Parts part in updateList.DeletedItems)
                {
                    try
                    {

                        var query = $@"DELETE FROM ""@@OCVTPARTS"" WHERE ""Code"" = @code AND ""U_SectionID"" = @sectionID";
                        using (var ppdb = new PetaPoco.Database(databaseConfig))
                        {
                            await ppdb.ExecuteScalarAsync<string>(query, new { code = part.Code, sectionID = part.SectionID });
                        }


                    }
                    catch (WebException wex)
                    {
                        if (wex.Status == WebExceptionStatus.ProtocolError)
                        {
                            var resp = wex.Response as HttpWebResponse;
                            if (resp == null || resp.StatusCode != HttpStatusCode.NotFound)
                            {
                                //Service layer can be weird when creating table - slow down, give it some time
                                await Task.Delay(1000);
                                continue;
                            }
                        }
                    }
                    



                }
            }
            //to update records one by one
            if (updateList.ChangedItems.Count > 0)
            {
                foreach (Parts part in updateList.ChangedItems)
                {


                    try
                    {
                        var Code = part.PartNo;
                        var section_ID = part.SectionID;
                        var query = @"UPDATE ""@@OCVTPARTS"" SET ""U_AssyRefID"" = @assy, ""U_Description"" = @desc, ""U_ConditionID"" = @cond, ""U_Fixing"" = @fix, ""U_DryFit"" = @dryfit, ""U_Weight"" = @weight, ""U_StorageBoxID"" = @box WHERE ""Code"" = @code";
                        using (var ppdb = new PetaPoco.Database(databaseConfig))
                        {
                            await ppdb.ExecuteAsync(query, new { code = part.Code, sectionID=part.SectionID, assy = part.AssyRef, desc = part.Description, cond = part.Condition, fix = part.Fixing, dryfit = part.DryFit, weight = part.Weight, box = part.StorageBox });
                        }



                    }
                    catch (WebException wex)
                    {
                        if (wex.Status == WebExceptionStatus.ProtocolError)
                        {
                            var resp = wex.Response as HttpWebResponse;
                            if (resp == null || resp.StatusCode != HttpStatusCode.NotFound)
                            {
                                //Service layer can be weird when creating table - slow down, give it some time
                                await Task.Delay(1000);
                                continue;
                            }
                        }
                    }

                }
            }

            if (updateList.TreatedAddedItems.Count > 0)
            {
                foreach (TreatedParts part in updateList.TreatedAddedItems)
                {


                    try
                    {
                        dynamic fields = new ExpandoObject();
                        string table = "U_OCHVTPARTTREATMENTS";
                        fields.Name = part.Treatment;
                        fields.U_PartID = part.PartNo;
                        fields.U_TreatmentID = part.Treatment;
                        fields.U_Description = part.TreatmentDescription;
                        fields.U_Quantity = part.Notes;
                        fields.U_Notes = part.Quantity;
                        //fields.U_Supplier = part.Supplier;
                        //fields.U_PO = part.PO;
                        fields.U_Status = part.Status;
                        fields.U_TreatmentOrder = part.Index;

                        BatchInstruction batchInstruction = new BatchInstruction();
                        batchInstruction.method = "POST";
                        batchInstruction.payload = fields;
                        batchInstruction.objectName = table;
                        var result = await client.executeSingleAsync(batchInstruction, false);
                    }
                    catch (WebException wex)
                    {
                        if (wex.Status == WebExceptionStatus.ProtocolError)
                        {
                            var resp = wex.Response as HttpWebResponse;
                            if (resp == null || resp.StatusCode != HttpStatusCode.NotFound)
                            {
                                //Service layer can be weird when creating table - slow down, give it some time
                                await Task.Delay(1000);
                                continue;
                            }
                        }
                    }

                }
            }

            //to delete records one by one
            if (updateList.TreatedDeletedItems.Count > 0)
            {
                foreach (TreatedParts part in updateList.TreatedDeletedItems)
                {
                    try
                    {
                        string table = "U_OCHVTPARTTREATMENTS";
                        var Code = part.PartNo;
                        var query = $@"DELETE FROM ""@@OCHVTPARTTREATMENTS"" WHERE ""Code"" = @code";
   
                        using (var ppdb = new PetaPoco.Database(databaseConfig))
                        {
                            await ppdb.ExecuteScalarAsync<string>(query, new { code = part.Code});
                        }




                    }
                    catch (WebException wex)
                    {
                        if (wex.Status == WebExceptionStatus.ProtocolError)
                        {
                            var resp = wex.Response as HttpWebResponse;
                            if (resp == null || resp.StatusCode != HttpStatusCode.NotFound)
                            {
                                //Service layer can be weird when creating table - slow down, give it some time
                                await Task.Delay(1000);
                                continue;
                            }
                        }
                    }

                }
            }

            //to update records one by one
            if (updateList.TreatedChangedItems.Count > 0)
            {
                foreach (TreatedParts part in updateList.TreatedChangedItems)
                {


                    try
                    {
                        var Code = part.PartNo;
                        var query = @"UPDATE ""@@OCHVTPARTTREATMENTS"" SET ""U_TreatmentID"" = @tID, ""U_Description"" = @Tdesc, ""U_Notes"" = @notes, ""U_Supplier"" = @supplier, ""U_PO"" = @PO, ""U_Status"" = @status, ""U_TreatmentOrder"" = @tOrder WHERE ""Code"" = @code AND ""U_PartID"" = @tID";
                        using (var ppdb = new PetaPoco.Database(databaseConfig))
                        {
                            await ppdb.ExecuteAsync(query, new { code = part.Code, tID = part.PartNo, Tdesc = part.TreatmentDescription, notes = part.Notes, supplier = part.Supplier, PO = part.PO, status = part.Status, tOrder = part.Index });
                        }



                    }
                    catch (WebException wex)
                    {
                        if (wex.Status == WebExceptionStatus.ProtocolError)
                        {
                            var resp = wex.Response as HttpWebResponse;
                            if (resp == null || resp.StatusCode != HttpStatusCode.NotFound)
                            {
                                //Service layer can be weird when creating table - slow down, give it some time
                                await Task.Delay(1000);
                                continue;
                            }
                        }
                    }

                }
            }
        }
        //for login purposes
        //[HttpGet(" / api/Parts/Login")]
        public async Task<bool> Login()
        {

            //check the credentials by logging in to service layer
            
            try
            {
                company = GetSLClient();
                return true;
            }
            catch (ServiceLayerSecurityException)
            {
                //we're actively looking for the security exception - this suggests that login failed
                await HttpContext.SignOutAsync();
                return false;
            }
             
        }
        
        public async Task prepareEnviroment()
        {

            //////////////////////////////////////////////////////////////////dummy data
            List<UDT> requireTables = new List<UDT>();
            List<UDF> requireFields = new List<UDF> {
            new UDF{TableName = "@BBB", FieldName = "ID", Description ="ID", Type= "Alpha", Size = 50, ForeignKey = ""},
            new UDF{TableName = "@BBB", FieldName = "Description", Description ="Description", Type= "Alpha", Size = 254, ForeignKey = ""},
            new UDF{TableName = "@BBB", FieldName = "Customer", Description ="Customer", Type= "Alpha", Size = 254, ForeignKey = ""},

            };
            //List<UDF> requireFields2 = new List<UDF> {
            //new UDF{Name = "ProjectID", Description ="Description", Type= "Alpha", Size = 50, ForeignKey = ""},
            //new UDF{Name = "ParentID", Description ="Customer", Type= "Alpha", Size = 254, ForeignKey = ""},
            //new UDF{Name = "Description", Description ="Description", Type= "Alpha", Size = 254, ForeignKey = ""},

            //};
            UDT newData0 = new UDT() { Name = "BBB", Description = "Parts Tracker Projects", Type = "NoObjectAutoIncrement", Udf = requireFields };
            UDT newData1 = new UDT() { Name = "BBB2", Description = "Parts Tracker Projects", Type = "NoObjectAutoIncrement", Udf = requireFields };
            UDT newData2 = new UDT() { Name = "BBB3", Description = "Parts Tracker", Type = "NoObjectAutoIncrement", Udf = requireFields };
            requireTables.Add(newData0);
            requireTables.Add(newData1);
            requireTables.Add(newData2);
            UDTs = requireTables;
            UDFs = requireFields;
            //////////////////////////////////////////////////////////////////
            var connectionEstablished = await Login();
            if (connectionEstablished == true)
            {

                //execute the udt creation file here


                //List<UDT> tablesNotCreated = requireTables.Where(x => x.Created == false).ToList();
                List<UDT> tablesNotCreated = new List<UDT>();
                bool allTablesDone = false;
                //only filtering the tables that have not yet been created
                foreach (var item in requireTables)
                {
                    if (item.Created == false)
                    {
                        tablesNotCreated.Add(new UDT
                        {
                            Name = item.Name,
                            Description = item.Description,
                            Type = item.Type,
                            Udf = item.Udf,
                            Created = item.Created,
                        });
                    }
                }
                //if all tables are not done yet we go back to tablecreater with the once that are not done yet
                /////////TableCreater(tablesNotCreated); 
               // await StartAsync();






            }
        }
        public async Task StartAsync()
        {
                    
    Task.Run(async () =>
            {
            List<UDTConfig> RequiredTables = new List<UDTConfig>()
        {
          new UDTConfig()
          {
            table = "OCHVTPROJECTS",
            description = "Parts Tracker Projects",
             autoinc = true
                    },
          new UDTConfig()
          {
            table = "OCHVTSECTIONS",
            description = "Parts Tracker",
             autoinc = true
                    },
           new UDTConfig()
          {
            table = "OCHVTASSEMREFS",
            description = "Parts Tracker Assembly Refs",
             autoinc = true
                    },
            new UDTConfig()
          {
            table = "OCHVTCONDITIONS",
            description = "Parts Tracker Conditions",
             autoinc = true
                    },
             new UDTConfig()
          {
            table = "OCHVTSTORAGEBOXES",
            description = "Parts Tracker Storage Boxes",
             autoinc = true
                    },
              new UDTConfig()
          {
            table = "OCHVTTREATMENTS",
            description = "Parts Tracker Treatments",
             autoinc = true
                    },
               new UDTConfig()
          {
            table = "OCVTPARTS",
            description = "Parts Tracker Parts",
             autoinc = true
                    },
                new UDTConfig()
          {
            table = "OCHVTPARTTREATMENTS",
            description = "Parts Tracker Treatments",
             autoinc = true
                    },

           };
            
            List < UDFConfig > RequiredFields = new List<UDFConfig>()
   {
               new UDFConfig()
                {
                                    table = "@OCHVTPROJECTS",
                                    field = "ID",
                                    description = "ID",
                                    type = UDFConfig.db_Alpha,
                                    size = 50
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTPROJECTS",
                                    field = "Description",
                                    description = "Description",
                                    type = UDFConfig.db_Alpha,
                                    size = 254
                }
                ,
                new UDFConfig()
                {
                                    table = "@OCHVTPROJECTS",
                                    field = "Customer",
                                    description = "Customer",
                                    type = UDFConfig.db_Alpha,
                                    size = 254
                },//////////////////////////////
                new UDFConfig()
                {
                                    table = "@OCHVTSECTIONS",
                                    field = "ProjectID",
                                    description = "Description",
                                    type = UDFConfig.db_Alpha,
                                    size = 50
                 },new UDFConfig()
                {
                                    table = "@OCHVTSECTIONS",
                                    field = "ParentID",
                                    description = "Customer",
                                    type = UDFConfig.db_Alpha,
                                    size = 50
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTSECTIONS",
                                    field = "Description",
                                    description = "Description",
                                    type = UDFConfig.db_Alpha,
                                    size = 254
                 },///////////////////////////////
                new UDFConfig()
                {
                                    table = "@OCHVTASSEMREFS",
                                    field = "ID",
                                    description = "ID",
                                    type = UDFConfig.db_Alpha,
                                    size = 50
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTASSEMREFS",
                                    field = "Description",
                                    description = "Description",
                                    type = UDFConfig.db_Alpha,
                                    size = 254
                 },///////////////////////////////
                new UDFConfig()
                {
                                    table = "@OCHVTCONDITIONS",
                                    field = "ID",
                                    description = "ID",
                                    type = UDFConfig.db_Alpha,
                                    size = 50
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTCONDITIONS",
                                    field = "Description",
                                    description = "Description",
                                    type = UDFConfig.db_Alpha,
                                    size = 254
                 },///////////////////////////////
                new UDFConfig()
                {
                                    table = "@OCHVTSTORAGEBOXES",
                                    field = "ID",
                                    description = "ID",
                                    type = UDFConfig.db_Alpha,
                                    size = 50
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTSTORAGEBOXES",
                                    field = "Description",
                                    description = "Description",
                                    type = UDFConfig.db_Alpha,
                                    size = 254
                 },///////////////////////////////
                new UDFConfig()
                {
                                    table = "@OCHVTTREATMENTS",
                                    field = "ID",
                                    description = "ID",
                                    type = UDFConfig.db_Alpha,
                                    size = 50
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTTREATMENTS",
                                    field = "Description",
                                    description = "Description",
                                    type = UDFConfig.db_Alpha,
                                    size = 254
                 },///////////////////////////////
                new UDFConfig()
                {
                                    table = "@OCVTPARTS",
                                    field = "SectionID",
                                    description = "Description",
                                    type = UDFConfig.db_Alpha,
                                    size = 254
                 },
                new UDFConfig()
                {
                                    table = "@OCVTPARTS",
                                    field = "AssyRefID",
                                    description = "Assembly Ref",
                                    type = UDFConfig.db_Alpha,
                                    size = 50
                 },
                new UDFConfig()
                {
                                    table = "@OCVTPARTS",
                                    field = "PartNo",
                                    description = "Part No",
                                    type = UDFConfig.db_Alpha,
                                    size = 10
                 },
                new UDFConfig()
                {
                                    table = "@OCVTPARTS",
                                    field = "Description",
                                    description = "Description",
                                    type = UDFConfig.db_Alpha,
                                    size = 254
                 },
                new UDFConfig()
                {
                                    table = "@OCVTPARTS",
                                    field = "Quantity",
                                    description = "Quantity",
                                    type = UDFConfig.db_Float,
                                    subtype = UDFConfig.st_Quantity,
                                    
                 },
                new UDFConfig()
                {
                                    table = "@OCVTPARTS",
                                    field = "ConditionID",
                                    description = "Condition ID",
                                    type = UDFConfig.db_Alpha,
                                    size = 50
                 },
                new UDFConfig()
                {
                                    table = "@OCVTPARTS",
                                    field = "Fixing",
                                    description = "Fixing",
                                    type = UDFConfig.db_Alpha,
                                    size = 10
                 },
                new UDFConfig()
                {
                                    table = "@OCVTPARTS",
                                    field = "DryFit",
                                    description = "DryFit",
                                    type = UDFConfig.db_Alpha,
                                    size = 10
                 },
                new UDFConfig()
                {
                                    table = "@OCVTPARTS",
                                    field = "Weight",
                                    description = "Weight",
                                    type = UDFConfig.db_Alpha,
                                    size = 10
                 },
                new UDFConfig()
                {
                                    table = "@OCVTPARTS",
                                    field = "StorageBoxID",
                                    description = "Storage Box ID",
                                    type = UDFConfig.db_Alpha,
                                    size = 50
                 },//////////////////////////////////
                new UDFConfig()
                {
                                    table = "@OCHVTPARTTREATMENTS",
                                    field = "PartID",
                                    description = "Part ID",
                                    type = UDFConfig.db_Alpha,
                                    size = 50
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTPARTTREATMENTS",
                                    field = "TreatmentID",
                                    description = "Treatment ID",
                                    type = UDFConfig.db_Alpha,
                                    size = 50
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTPARTTREATMENTS",
                                    field = "Description",
                                    description = "Description",
                                    type = UDFConfig.db_Alpha,
                                    size = 254
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTPARTTREATMENTS",
                                    field = "Quantity",
                                    description = "Quantity",
                                    type = UDFConfig.db_Numeric,
                                    size = 10
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTPARTTREATMENTS",
                                    field = "Notes",
                                    description = "Notes",
                                    type = UDFConfig.db_Alpha,
                                    size = 254
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTPARTTREATMENTS",
                                    field = "Supplier",
                                    description = "Supplier",
                                    type = UDFConfig.db_Alpha,
                                    size = 50
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTPARTTREATMENTS",
                                    field = "PO",
                                    description = "PO",
                                    type = UDFConfig.db_Numeric,
                                    size = 10
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTPARTTREATMENTS",
                                    field = "Status",
                                    description = "Status",
                                    type = UDFConfig.db_Alpha,
                                    size = 10
                 },
                new UDFConfig()
                {
                                    table = "@OCHVTPARTTREATMENTS",
                                    field = "TreatmentOrder",
                                    description = "Treatment Order",
                                    type = UDFConfig.db_Numeric,
                                    size = 10
                 },
    };
            _logger.LogInformation("InitializeUDTsService: Creating Service Layer Client");
                Client client;
                if (company != null)
                {
                    client = company;
                }
                else
                {
                    client = GetSLClient();
                }
                var AnyErrors = true;
                var tablestate = RequiredTables?.Select(rt => new TableTrack(rt)).ToList();
                var fieldstate = RequiredFields?.Select(rf => new FieldTrack(rf)).ToList();
                //var settingstate = _dbconf.RequiredOchAppCfgSettings?.Select(rs => new SettingTrack(rs)).ToList();

                while (AnyErrors || tablestate.Any(ts => !ts.Created) || fieldstate.Any(fs => !fs.Created))
                {
                    AnyErrors = false;

                    if (tablestate?.Any(ts => !ts.Created) ?? false)
                    {
                        _logger.LogInformation($"InitializeUDTsService: {tablestate.Count(ts => !ts.Created)} tables(s) to check.");

                        foreach (var tabletrack in tablestate.Where(ts => !ts.Created))
                        {
                            var table = tabletrack.Config;
                            //if (cancellationToken.IsCancellationRequested)
                            //{
                            //    _logger.LogInformation($"InitializeUDTsService: Cancel Requested, Returning");
                            //    return;
                            //}
                            _logger.LogInformation($"InitializeUDTsService: Considering table {table.table}");
                            bool tablepresent = false;
                            try
                            {
                                var ins = new BatchInstruction();
                                ins.method = "GET";
                                ins.objectName = $"UserTablesMD?$filter=TableName eq '{table.table}'";
                                var sfieldinfo = await client.executeSingleAsync(ins, false);
                                var jfieldinfo = JObject.Parse(sfieldinfo);
                                tablepresent = ((JArray)jfieldinfo["value"])?.Count != 0;
                            }
                            catch (WebException wex)
                            {
                                if (wex.Status == WebExceptionStatus.ProtocolError)
                                {
                                    var resp = wex.Response as HttpWebResponse;
                                    if (resp == null || resp.StatusCode != HttpStatusCode.NotFound)
                                    {
                                        //Service layer can be weird when creating table - slow down, give it some time
                                        await Task.Delay(1000);
                                        AnyErrors = true;
                                        continue;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"InitializeUDTsService: Checking table {table.table}", ex);
                                //Service layer can be weird when creating table - slow down, give it some time
                                await Task.Delay(1000);
                                AnyErrors = true;
                                continue;
                            }
                            _logger.LogInformation($"InitializeUDTsService: Table {table.table}");
                            if (tablepresent)
                            {
                                tabletrack.Created = true;
                            }
                            else
                            {
                                try
                                {
                                    await client.executeSingleAsync(BatchInstruction.InsertUDTMD(table.table, table.description, "bott_NoObjectAutoIncrement"));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"InitializeUDTsService: Creating table {table.table}", ex);
                                    //Service layer can be weird when creating field - slow down, give it some time
                                    await Task.Delay(1000);
                                    AnyErrors = true;
                                }
                            }
                        }
                    }

                    if (fieldstate?.Any(fs => !fs.Created) ?? false)
                    {
                        _logger.LogInformation($"InitializeUDTsService: {fieldstate.Count(fs => !fs.Created)} field(s) to check.");

                        foreach (var fieldtracker in fieldstate.Where(fs => !fs.Created))
                        {
                            var field = fieldtracker.Config;
                         
                            _logger.LogInformation($"InitializeUDTsService: Considering field {field.field} on table {field.table}");
                            bool fieldpresent = false;
                            try
                            {
                                var ins = new BatchInstruction();
                                ins.method = "GET";
                                ins.objectName = $"UserFieldsMD?$filter=TableName eq '{field.table}' and Name eq '{field.field}'";
                                var sfieldinfo = await client.executeSingleAsync(ins, false);
                                var jfieldinfo = JObject.Parse(sfieldinfo);
                                fieldpresent = ((JArray)jfieldinfo["value"])?.Any() ?? false;
                            }
                            catch (WebException wex)
                            {
                                if (wex.Status == WebExceptionStatus.ProtocolError)
                                {
                                    var resp = wex.Response as HttpWebResponse;
                                    if (resp == null || resp.StatusCode != HttpStatusCode.NotFound)
                                    {
                                        //Service layer can be weird when creating table - slow down, give it some time
                                        await Task.Delay(1000);
                                        AnyErrors = true;
                                        continue;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError($"InitializeUDTsService: Checking field {field.field} on table {field.table}", ex);
                                //Service layer can be weird when creating field - slow down, give it some time
                                await Task.Delay(1000);
                                AnyErrors = true;
                                continue;
                            }
                            _logger.LogInformation($"InitializeUDTsService: Field {field.field} on table {field.table}, found = {fieldpresent}");
                            if (fieldpresent)
                            {
                                fieldtracker.Created = true;
                            }
                            else
                            {
                                try
                                {
                                    await client.executeSingleAsync(BatchInstruction.InsertUDFMD(field, false));
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"InitializeUDTsService: Creating field {field.field} on table {field.table}", ex);
                                    //Service layer can be weird when creating field - slow down, give it some time
                                    await Task.Delay(1000);
                                    AnyErrors = true;
                                }
                            }
                        }
                    }

                    if (AnyErrors)
                    {
                        //Service layer can be weird when creating field - slow down, give it some time
                        await Task.Delay(10000);
                    }
                }
                _logger.LogInformation($"InitializeUDTsService: Complete.");
            })
                .ContinueWith((T) => {
                    _logger.LogError(T.Exception, "Fault in InitializeUDTsService");
                }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);

            //return Task.CompletedTask;
        }

        //public async Task<bool> quickTableCheck(string tableName)
        //{
        //    try { var r = await company.GetAsync("/UserTablesMD('" + tableName + "')"); }
        //    catch(Exception e) { return false; }
           
        //    return true;
        //}


        //public async Task TableCreater(List<UDT> requireTables) {
        //    //we check if a table exists or not
        //    //for (int i = 0; i <= requireTables.Count ; i++)
        //    foreach(UDT udt1 in requireTables)
        //    {
        //        //var udt = requireTables[i];
        //        var udt = udt1;
        //        if (udt.Created == false && allTablesDone == false)
        //        {
                     
        //            try
        //            {
        //                string tableToCheck = udt.Name.ToUpper();

        //                //we define the table here
        //                SingleUDT thisUDT = new SingleUDT();
        //                thisUDT.TableName = udt.Name;//requireTables[i].Name;
        //                thisUDT.TableDescription = udt.Description;//requireTables[i].Description;
        //                thisUDT.TableType = "bott_NoObject";//requireTables[i].Type;

                       
        //                string jj =  JsonConvert.SerializeObject(thisUDT).ToString();
        //                //we create the table here
        //                var res = await company.PostAsync("UserTablesMD",jj);
        //                //Thread.Sleep(5000);
        //                var result = false;
        //                var ins = new BatchInstruction();
        //                ins.method = "GET";
        //                ins.objectName = $"UserTablesMD?$filter=TableName eq '"+tableToCheck+"'";
        //                //Thread.Sleep(5000);
        //                var sfieldinfo = await company.executeSingleAsync(ins, false);
        //                var jfieldinfo = JObject.Parse(sfieldinfo);
        //                result = ((JArray)jfieldinfo["value"])?.Count != 0;
                        
                        
        //                if (result == true) 
        //                { 
        //                    udt.Created = true;
        //                    if (!requireTables.Any(x => x.Created == false)) { 
        //                        allTablesDone = true; 
        //                        break; }
        //                }
                       

        //            }
        //            catch (Exception e)
        //            {

        //            }
        //        }
        //    }
        // //return requireTables;

        //}



        //public async Task<List<UDT>> FieldCreater(List<UDT> requireTables)
        //{
        //    //we check if a table exists or not
        //    for (int i = 0; i <= requireTables.Count; i++)
        //    {
        //        var udt = requireTables[i];
        //        if (udt.Created == false && allTablesDone == false)
        //        {

        //            try
        //            {
        //                string tableToCheck = udt.Name.ToUpper();

        //                //we define the table here
        //                SingleUDT thisUDT = new SingleUDT();
        //                thisUDT.TableName = requireTables[i].Name;
        //                thisUDT.TableDescription = requireTables[i].Description;
        //                thisUDT.TableType = "bott_NoObject";//requireTables[i].Type;


        //                string jj = JsonConvert.SerializeObject(thisUDT).ToString();
        //                //we create the table here
        //                var res = company.PostAsync("UserFieldsMD", jj);
        //                Thread.Sleep(5000);
        //                var result = false;
        //                var ins = new BatchInstruction();
        //                ins.method = "GET";
        //                ins.objectName = $"UserFieldsMD?$filter=TableName eq '" + tableToCheck + "'";
        //                //Thread.Sleep(5000);
        //                var sfieldinfo = await company.executeSingleAsync(ins, false);
        //                var jfieldinfo = JObject.Parse(sfieldinfo);
        //                result = ((JArray)jfieldinfo["value"])?.Count != 0;


        //                if (result == true)
        //                {
        //                    udt.Created = true;
        //                    if (!requireTables.Any(x => x.Created == false))
        //                    {
        //                        allTablesDone = true;
        //                        break;
        //                    }
        //                }


        //            }
        //            catch (Exception e)
        //            {

        //            }
        //        }
        //    }
        //    return requireTables;
        //now do this again to do any of remaining once and keep looping until all done marker is set to true

        //}























        //to get/check a specific part

        [HttpGet("/api/Parts/getParts")]
        public async Task<IEnumerable<string>> getParts()
        {
            return new string[] { "value1", "value2" };

        }
    }
}

using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using SAPB1Commons.ServiceLayer;
using Task = System.Threading.Tasks.Task;
using System.Net;
using LunazVehiclePartsTracker.Data;

namespace LunazVehiclePartsTracker.Code
{
    public class InitializeUDTsServiceCompleteHolder
    {
        private TaskCompletionSource TaskCompletionSource;

        public InitializeUDTsServiceCompleteHolder()
        {
            TaskCompletionSource = new TaskCompletionSource();
            IsComplete = TaskCompletionSource.Task;
        }
        public Task IsComplete { get; set; }

        public void Complete()
        {
            TaskCompletionSource.SetResult();
        }
    }

    public class TableTrack
    {
        public TableTrack() { }
        public TableTrack(UDTConfig config)
        {
            Config = config;
        }

        public bool Created { get; set; } = false;
        public UDTConfig Config { get; set; }
    }

    public class FieldTrack
    {
        public FieldTrack() { }
        public FieldTrack(UDFConfig config)
        {
            Config = config;
        }

        public bool Created { get; set; } = false;
        public UDFConfig Config { get; set; }
    }

    public class SettingTrack
    {
        public SettingTrack() { }
        public SettingTrack(OchAppCfgSetting config)
        {
            Config = config;
        }

        public bool Created { get; set; } = false;
        public OchAppCfgSetting Config { get; set; }
    }

    public class InitializeUDTsService : IHostedService
    {
        private readonly UserDBConfig _dbconf;
        private readonly ILogger<InitializeUDTsService> _logger;
        private ConnectionPool _CM;
        private readonly IOptions<ConnectionDetails> _SLConnectionDetails;
        private readonly InitializeUDTsServiceCompleteHolder _completeHolder;

        //The Ready Event will only be set when UDT/UDF config is observed in the database
        public ManualResetEventSlim Ready = new ManualResetEventSlim(false);

        public InitializeUDTsService(ILogger<InitializeUDTsService> logger, ConnectionPool CM, UserDBConfig config, IOptions<ConnectionDetails> SLConnectionDetails, InitializeUDTsServiceCompleteHolder completeHolder)
        {
            this._dbconf = config;
            this._logger = logger;
            this._CM = CM;
            this._SLConnectionDetails = SLConnectionDetails;
            this._completeHolder = completeHolder;
        }

        //private Client GetSLClient()
        //{
        //    var CM = _CM;
        //    var config = _SLConnectionDetails;
        //    var url = config.Value.ServiceLayerURL;
        //    var companyDb = config.Value.DatabaseName;
        //    var username = config.Value.UserName;
        //    var password = config.Value.Password;
        //    return CM.GetConnection(url, companyDb, username, password);
        //}
        public Client GetSLClient()
        {
            var DBServerType = SAPB1Commons.B1Types.DatabaseType.MsSql;
            var conf = new SAPB1Commons.B1Types.B1DirectDBProfile() { DatabaseName = "HW_TEST_NEW", DBPassword = "0ch1ba2021!", DBServerName = "OBSLDEV-1", DBType = DBServerType, DBUserName = "sa", ServiceLayerURL = "https://OBSLDEV-1:50000/b1s/v1/", DBTenantName = "" };
            var databaseConfig = SAPB1Commons.PetaPocoConnectionBuilder.BuildSAPBusinessOneConfigForPetaPoco(conf);

            //_logger.LogInformation("Creating Service Layer Client");
            var CM = _CM;
            var config = conf;
            var url = "https://localhost:50000/b1s/v1/";
            var companyDb = "HW_TEST_NEW";
            return CM.GetConnection(url, companyDb, "manager", "H4rt3W", false);
        }

    public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                _logger.LogInformation("InitializeUDTsService: Creating Service Layer Client");
                var client = GetSLClient();

                var AnyErrors = true;
                var tablestate = _dbconf.RequiredTables?.Select(rt => new TableTrack(rt)).ToList();
                var fieldstate = _dbconf.RequiredFields?.Select(rf => new FieldTrack(rf)).ToList();
                var settingstate = _dbconf.RequiredOchAppCfgSettings?.Select(rs => new SettingTrack(rs)).ToList();

                while (AnyErrors || tablestate.Any(ts => !ts.Created) || fieldstate.Any(fs => !fs.Created) || settingstate.Any(rs => !rs.Created))
                {
                    AnyErrors = false;

                    if (tablestate?.Any(ts => !ts.Created) ?? false)
                    {
                        _logger.LogInformation($"InitializeUDTsService: {tablestate.Count(ts => !ts.Created)} tables(s) to check.");

                        foreach (var tabletrack in tablestate.Where(ts => !ts.Created))
                        {
                            var table = tabletrack.Config;
                            if (cancellationToken.IsCancellationRequested)
                            {
                                _logger.LogInformation($"InitializeUDTsService: Cancel Requested, Returning");
                                return;
                            }
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
                                    await client.executeSingleAsync(BatchInstruction.InsertUDTMD(table.table, table.description));
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
                            if (cancellationToken.IsCancellationRequested)
                            {
                                _logger.LogInformation($"InitializeUDTsService: Cancel Requested, Returning");
                                return;
                            }
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
                _completeHolder.Complete();
            }, cancellationToken)
                .ContinueWith((T) => {
                    _logger.LogError(T.Exception, "Fault in InitializeUDTsService");
                }, System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            //TODO: stop gracefully
            return Task.Delay(1);
        }

        private class OchAppCfg
        {
            public OchAppCfg() { }
            public OchAppCfg(OchAppCfgSetting st)
            {
                U_ProgID = st.progid;
                U_Module = st.module;
                U_Type = st.type;
                U_Description = st.description;
                U_Delimiter = st.delimiter;
                U_Query = st.query;
                U_ConfigData = st.configdata;
                U_ExtConfigData = st.extconfigdata;
                U_Forms = st.forms;
            }
            public string Code { get; set; }
            public string Name { get; set; }
            public string U_ProgID { get; set; }
            public string U_Module { get; set; }
            public string U_Type { get; set; }
            public string U_Description { get; set; }
            public string U_Delimiter { get; set; }

            public string U_Query { get; set; }
            public string U_ConfigData { get; set; }
            public string U_ExtConfigData { get; set; }
            public string U_Forms { get; set; }
        }

        private class ODataOchAppCfgWrapper
        {
            public List<OchAppCfg> value { get; set; }
        }
    }
}

using System.Net;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace SAPB1Commons.ServiceLayer
{
    public class Client
    {
        //If a page size is not specified by the caller, use this number
        //Note: This number should be in the tens or hundreds. The practical upper limit for ServiceLayer is
        //around 3000-5000, however a balance should be struck between our async approach and the page size
        public const int DEFAULT_ODATA_MAXPAGESIZE = 200;

        private ConnectionPool containingConnectionPool = null;

        public string ServerUrl { get; set; } = null;
        public string CompanyDB { get; set; } = null;
        public string UserName { get; set; } = null;
        public string Password { get; set; } = null;
        public DateTime LastConnectTime { get; set; }
        public DateTime ExpectedExpiryTime { get; set; }
        public int RequestTimoutSeconds { get; set; } = 0;

        private ILogger<Client> logger;

        public Connection Connection { get; set; } = null;

        public class ReportingConfig
        {
            /// <summary>
            /// Requests via this library that end in a fault condition will attempt to parse the error response
            /// if the error is correctly parsed then an instance of Client.SAPError is thrown instead of System.Net.WebException
            /// </summary>
            public bool EnableSapErrorParsing { get; set; } = false;
        }
        private readonly ReportingConfig _repconf = new ReportingConfig();

        public class SAPErrorException : Exception
        {
            public int Code { get; set; }

            public SAPErrorException(int Code, string Message) : base(Message)
            {
                this.Code = Code;
            }
            public SAPErrorException(int Code, string Message, Exception inner) : base(Message, inner)
            {
                this.Code = Code;
            }
            public static async Task<SAPErrorException> TryParse(ILogger logger, System.Net.WebException webException)
            {
                try
                {
                    using (var rd = new StreamReader(webException.Response.GetResponseStream()))
                    {
                        var RawResponse = await rd.ReadToEndAsync();

                        logger.LogError(webException, $"Raw Response: {RawResponse}");

                        var jobj = JObject.Parse(RawResponse);
                        var TCode = jobj.SelectToken("error.code");
                        var TMessage = jobj.SelectToken("error.message.value");

                        if (TCode == null || TMessage == null)
                        {
                            return null;
                        }
                        return new SAPErrorException(TCode.ToObject<int>(), TMessage.ToObject<string>(), webException);
                    }
                } catch
                {
                    //We're already in an error handler - ignore any error here and indicate that we can't produce a SAPErrorException
                    return null;
                }
            }
            public override string ToString()
            {
                return $"Code: {Code} Message: {Message}";
            }
        }

        public Client(ILogger<Client> nlogger)
        {
            logger = nlogger;

        }

        public Client(ILogger<Client> nlogger, Client.ReportingConfig repconf) : this(nlogger)
        {
            _repconf = repconf;
        }

        public void SetConnectionDetails(string ServerUrl, string CompanyDB, string UserName, string Password)
        {
            this.ServerUrl = ServerUrl;
            if (!this.ServerUrl.EndsWith("/")) this.ServerUrl += "/";
            this.CompanyDB = CompanyDB;
            this.UserName = UserName;
            this.Password = Password;
        }

        //todo: This refactoring to allow logon by ContextCookie feels a bit awkward and maybe should be reconsidered
        public void SetConnectionDetailsWithCookieString(string ServerUrl, string CompanyDB, string UserName, string PackedCookies)
        {
            this.ServerUrl = ServerUrl;
            this.CompanyDB = CompanyDB;
            this.UserName = UserName;
            this.Password = "";

            Connection = new Connection();

            var cookies = new Dictionary<string, string>();
            string[] cookieItems = PackedCookies.Split(';');
            foreach (var cookieItem in cookieItems)
            {
                string[] parts = cookieItem.Split('=');
                if (parts.Length == 2)
                {
                    switch (parts[0].Trim())
                    {
                        case "ROUTEID":
                            Connection.ROUTEID = parts[1].Trim();
                            break;

                        case "B1SESSION":
                            Connection.SessionId = parts[1].Trim();
                            break;
                    }
                }
            }

            if (String.IsNullOrEmpty(Connection.ROUTEID))
            {
                throw new Exception("No ROUTEID cookie was provided to SetConnectionDetailsWithCookieString()");
            }

            if (String.IsNullOrEmpty(Connection.SessionId))
            {
                throw new Exception("No B1SESSION cookie was provided to SetConnectionDetailsWithCookieString()");
            }
        }

        public void SetConnectionDetailsWithCookieString(string ServerUrl, string CompanyDB, string UserName, string B1SESSION, string ROUTEID)
        {
            this.ServerUrl = ServerUrl;
            this.CompanyDB = CompanyDB;
            this.UserName = UserName;
            this.Password = "";

            Connection = new Connection();
            Connection.ROUTEID = ROUTEID;
            Connection.SessionId = B1SESSION;

            if (String.IsNullOrEmpty(Connection.ROUTEID))
            {
                throw new Exception("No ROUTEID cookie was provided to SetConnectionDetailsWithCookieString()");
            }

            if (String.IsNullOrEmpty(Connection.SessionId))
            {
                throw new Exception("No B1SESSION cookie was provided to SetConnectionDetailsWithCookieString()");
            }
        }

        public Client(ILogger<Client> nlogger, string ServerUrl, string CompanyDB, string UserName, string Password) : this(nlogger)
        {
            logger.LogInformation("Creating a service layer client for " + UserName + " on " + ServerUrl);
            SetConnectionDetails(ServerUrl, CompanyDB, UserName, Password);
        }

        public Client(ILogger<Client> nlogger, string ServerUrl, string CompanyDB, string UserName, string RouteId, string SessionId) : this(nlogger)
        {
            logger.LogInformation("Creating a service layer client for " + UserName + " with existing session token on " + ServerUrl);
            SetConnectionDetails(ServerUrl, CompanyDB, UserName, "");

            Connection = new Connection();
            Connection.ROUTEID = RouteId;
            Connection.SessionId = SessionId;
        }

        public Client(ILogger<Client> nlogger, ConnectionCredentials creds) : this(nlogger)
        {
            SetConnectionDetails(creds.ServerUrl, creds.CompanyDB, creds.UserName, creds.Password);
        }

        public void LogSLRequest(string method, string url, string body)
        {
#if LOGSL
            logger.LogInformation(string.Format("SERVICE LAYER REQUEST: URL {0} REQUEST BODY {1}", url, body));
#endif
        }

        public void LogSLResponse(string method, string url, string body)
        {
#if LOGSL
            //full response will be logged again on error, only need about 500 chars for general case
            if (body.Length >= 500) body = body.Substring(0, 500);
            logger.LogInformation(string.Format("SERVICE LAYER RESPONSE: URL {0} RESPONSE BODY {1}", url, body));
#endif
        }

        public bool connect()
        {
            try
            {
                string webAddr = ServerUrl + "Login";

                //Create the request.
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);

                //Setup the request.
                httpWebRequest.ContentType = "application/json; charset=utf-8";
                httpWebRequest.Method = "POST";
                httpWebRequest.ServerCertificateValidationCallback = delegate { return true; };
                httpWebRequest.ServicePoint.Expect100Continue = false;
                //we don't use a non-standard request timeout for Login

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    dynamic con = new JObject();
                    con.CompanyDB = CompanyDB;
                    con.UserName = UserName;
                    con.Password = Password;

                    LogSLRequest(httpWebRequest.Method, webAddr, con.ToString());
                    streamWriter.Write(con);
                    streamWriter.Flush();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();
                    LogSLResponse(httpWebRequest.Method, webAddr, responseText);

                    Connection = Newtonsoft.Json.JsonConvert.DeserializeObject<Connection>(responseText);

                    Connection.SessionStart = DateTime.UtcNow;

                    string strMessage = httpResponse.GetResponseHeader("Set-Cookie");

                    if (false == string.IsNullOrEmpty(strMessage))
                    {
                        //The ROUTEID information will be returned during login, if sever is configured to be "Clustered" Mode.
                        int idx = strMessage.IndexOf("ROUTEID=");
                        if (idx > 0)
                        {
                            string strSubString = strMessage.Substring(idx);
                            int idxSplitter = strSubString.IndexOf(";");
                            if (idxSplitter > 0)
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(strSubString.Substring(0, idxSplitter), "ROUTEID=\\s*(?<routeid>[^$;]*)");
                                if (match.Success)
                                {
                                    Connection.ROUTEID = match.Groups["routeid"].Value.Trim();
                                }
                                else
                                {
                                    Connection = null;
                                    throw new Exception("Bad Response from Service Layer - Could not find ROUTEID");
                                }
                            }
                            else
                            {
                                Connection = null;
                                throw new Exception("Bad Response from Service Layer - Could not find ;");
                            }
                        }
                    }

                    logger.LogDebug(string.Format("Service Layer Connection: B1SESSION={0}; ROUTEID={1}", Connection.SessionId, Connection.ROUTEID));
                    return true;
                }
            }
            catch (WebException we)
            {
                LogWebException(we);
                return false;
            }
        }

        public bool checkCredentials(string Username, string Password)
        {
            try
            {
                string webAddr = ServerUrl + "Login";

                //Create the request.
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(webAddr);

                //Setup the request.
                httpWebRequest.ContentType = "application/json; charset=utf-8";
                httpWebRequest.Method = "POST";
                httpWebRequest.ServerCertificateValidationCallback = delegate { return true; };
                httpWebRequest.ServicePoint.Expect100Continue = false;

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    dynamic con = new JObject();
                    con.CompanyDB = CompanyDB;
                    con.UserName = UserName;
                    con.Password = Password;

                    con.UserName = Username;
                    con.Password = Password;

                    LogSLRequest(httpWebRequest.Method, webAddr, con.ToString());
                    streamWriter.Write(con.ToString());
                    streamWriter.Flush();
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var responseText = streamReader.ReadToEnd();
                    LogSLResponse(httpWebRequest.Method, webAddr, responseText);
                    var probeconnection = new Connection();
                    probeconnection = Newtonsoft.Json.JsonConvert.DeserializeObject<Connection>(responseText);
                    return probeconnection != null && !string.IsNullOrWhiteSpace(probeconnection.SessionId);
                }
            }
            catch (WebException ex)
            {
                logger.LogWarning(ex, "Login Credentials incorrect during login.");
                LogWebException(ex);

                return false;
            }
        }

        public async Task<string> DirectRequestAsync(string url, string body, string method = "POST", int? odatamaxpagesize = null, bool ReplaceCollectionsOnPatch = false, int OverrideRequestTimeoutSeconds = 0)
        {
            var (value, _) = await DirectRequestWithStatusAsync(url, body, method, odatamaxpagesize, ReplaceCollectionsOnPatch, OverrideRequestTimeoutSeconds);
            return value;
        }
        public async Task<(string, HttpStatusCode)> DirectRequestWithStatusAsync(string url, string body, string method = "POST", int? odatamaxpagesize = null, bool ReplaceCollectionsOnPatch = false, int OverrideRequestTimeoutSeconds = 0)
        {
            logger.LogInformation("DirectRequest() " + method + " to " + url);

            //In case the session has timed out we'll check to see if we need to request a new service layer token
            if (DateTime.UtcNow > Connection.SessionStart.AddMinutes(Connection.SessionTimeout / 2))
            {
                connect();
            }

            //Create a request and specify the request url.
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            if (odatamaxpagesize.HasValue)
            {
                request.Headers.Add("Prefer", "odata.maxpagesize=" + odatamaxpagesize);
            } else
            {
                //Default seemed to be 20 records before - not sure that 20 records is appropraite for a data connector
                request.Headers.Add("Prefer", "odata.maxpagesize=" + DEFAULT_ODATA_MAXPAGESIZE);
            }
            if (ReplaceCollectionsOnPatch) request.Headers.Add("B1S-ReplaceCollectionsOnPatch", "true");

            //Setup the request.
            request.ServerCertificateValidationCallback = delegate { return true; };
            request.Method = method;
            request.ContentType = "application/json: charset=utf-8";
            request.ServicePoint.Expect100Continue = false;
            if (RequestTimoutSeconds > 0) { request.Timeout = RequestTimoutSeconds * 1000; }
            if (OverrideRequestTimeoutSeconds > 0) { request.Timeout = OverrideRequestTimeoutSeconds * 1000; }

            var sl = (new System.Uri(url, UriKind.Absolute)).Host;

            //Set cookies, session id essentially gives us the accesss ability to post to the database.
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(new Cookie("B1SESSION", Connection.SessionId, "", sl));
            request.CookieContainer.Add(new Cookie("ROUTEID", Connection.ROUTEID, "", sl));

            //write the json data to the body of the request.
            if (body != null)
            {
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    LogSLRequest(method, url, body);
                    streamWriter.Write(body);
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }

            //Collect the result and return true or false to the client to indicate if the post request was a success or not.

            logger.LogTrace("Request prepared, ready to execute HttpWebResponse.");

            try
            {
                var httpResponse = (HttpWebResponse)await request.GetResponseAsync();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    LogSLResponse(method, url, result);
                    this.LastConnectTime = DateTime.Now;
                    this.ExpectedExpiryTime = DateTime.Now.AddMinutes(Connection.SessionTimeout);

                    logger.LogTrace("DirectRequest returning " + result.ToString());

                    return (result, httpResponse.StatusCode);
                }
            }
            catch (WebException err) when (err.Status == WebExceptionStatus.Timeout)
            {
                logger.LogInformation("Timeout for request was: " + request.Timeout.ToString() + " milliseconds.");
                throw err;
            }
            catch (WebException err)
            {
                var sapex = await SAPErrorException.TryParse(logger, err);
                if (_repconf.EnableSapErrorParsing && sapex != null)
                {
                    throw sapex;
                }
                
                throw;
            }
        }

        public async Task<string> DirectPostAsync(string url, string body)
        {
            return await DirectRequestAsync (url, body);
        }
        public async Task<(string, HttpStatusCode)> DirectPostWithStatusAsync(string url, string body)
        {
            return await DirectRequestWithStatusAsync(url, body);
        }
        public async Task<string> PostAsync(string url, string body)
        {
            return await DirectRequestAsync(ServerUrl + url, body);
        }
        public async Task<string> DirectPutAsync(string url, string body)
        {
            return await DirectRequestAsync (url, body, "PUT");
        }
        public async Task<(string, HttpStatusCode)> DirectPutWithStatusAsync(string url, string body)
        {
            return await DirectRequestWithStatusAsync(url, body, "PUT");
        }
        public async Task<string> PutAsync(string url, string body)
        {
            return await DirectRequestAsync (ServerUrl + url, body, "PUT");
        }
        public async Task<string> DirectPatchAsync(string url, string body, bool ReplaceCollectionsOnPatch = false)
        {
            return await DirectRequestAsync(url, body, "PATCH", null, ReplaceCollectionsOnPatch);
        }
        public async Task<(string, HttpStatusCode)> DirectPatchWithStatusAsync(string url, string body, bool ReplaceCollectionsOnPatch = false)
        {
            return await DirectRequestWithStatusAsync(url, body, "PATCH", null, ReplaceCollectionsOnPatch);
        }
        public async Task<string> PatchAsync(string url, string body, bool ReplaceCollectionsOnPatch = false)
        {
            return await DirectRequestAsync(ServerUrl + url, body, "PATCH", null, ReplaceCollectionsOnPatch);
        }
        public async Task<string> DirectDeleteAsync(string url)
        {
            return await DirectRequestAsync(url, null, "DELETE");
        }
        public async Task<string> DeleteAsync(string url)
        {
            return await DirectRequestAsync(ServerUrl + url, null, "DELETE");
        }
        public async Task<string> DirectGetAsync(string url)
        {
            return await DirectRequestAsync(url, null, "GET");
        }
        public async Task<string> GetAsync(string url, int? odatamaxpagesize = null)
        {
            return await DirectRequestAsync(ServerUrl + url, null, "GET", odatamaxpagesize);
        }
        public async Task<string> CreateSAPBusinessOneObjectAsync(string objectName, string body)
        {
            return await DirectRequestAsync(ServerUrl + objectName, body);
        }

        public async Task<string> CreateSAPBusinessOneUDTAsync(string table, string description, string tableType = "bott_NoObject")
        {
            var body = JsonConvert.SerializeObject(new { TableName = table, TableDescription = description, TableType = tableType });
            return await DirectRequestAsync ("UserTablesMD", body);
        }

        private static bool IsValidJson(string strInput)
        {
            strInput = strInput.Trim();
            if ((strInput.StartsWith("{") && strInput.EndsWith("}")) || //For object
                (strInput.StartsWith("[") && strInput.EndsWith("]"))) //For array
            {
                try
                {
                    var obj = JToken.Parse(strInput);
                    return true;
                }
                catch (JsonReaderException jex)
                {
                    //Exception in parsing json
                    Console.WriteLine(jex.Message);
                    return false;
                }
                catch (Exception ex) //some other exception
                {
                    Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
        public async Task<string> executeSingleAsync(BatchInstruction item, bool WithRetry = true, int OverrideRequestTimeoutSeconds = 0)
        {
            logger.LogInformation("executeSingle() " + item.method + ", objectName: " + item.objectName + ", ReplaceCollectionsOnPatch: " + item.ReplaceCollectionsOnPatch);


            var body = item.payload == null ? (string)null : Newtonsoft.Json.JsonConvert.SerializeObject(item.payload);

            string url = ServerUrl + item.objectName;
            if (item.objectKey != null)
            {
                if (typeof(Int32).IsAssignableFrom(item.objectKey.GetType()) || typeof(UInt32).IsAssignableFrom(item.objectKey.GetType()) ||
                    typeof(Int64).IsAssignableFrom(item.objectKey.GetType()) || typeof(UInt64).IsAssignableFrom(item.objectKey.GetType()))
                {
                    url += string.Format("({0})", item.objectKey);
                }
                else
                {
                    url += string.Format("('{0}')", item.objectKey);
                }
            }

            try
            {
                var rawContent = await DirectRequestAsync(url, body, item.method, null, item.ReplaceCollectionsOnPatch, OverrideRequestTimeoutSeconds);

                //put the result back into the instruction
                var response = new SBOServiceLayerInstructionResponse();

                //todo: when using DirectRequest() to post a single instruction, we don't have access to the response object to get the protocol back.
                //response.HttpStatus = int.Parse(protocolstatus);
                //response.HttpStatusMessage = protocolmessage;
                response.RawContent = rawContent;

                JToken jsonContent = null;
                Exception jsonException = null;
                if (ServiceLayer.Client.IsValidJson(rawContent))
                {
                    try
                    {
                        jsonContent = JToken.Parse(rawContent);
                    }
                    catch (Exception ex)
                    {
                        jsonException = ex;
                        jsonContent = null;
                    }
                }

                response.JsonContent = jsonContent;
                response.JsonException = jsonException;
                item.Response = response;

                return rawContent;
            }
            catch (WebException err) when (err.Status == WebExceptionStatus.Timeout)
            {
                //dont retry if the error is a request timeout - it might still be going through and an arbitrary retry could result in the data being processed twice
                logger.LogError("executeSingle(): A WebException occured, request has timed out, throwing the error: " + err.ToString());

#if !LOGSL
                //the request hasn't been previously logged
                logger.LogInformation(string.Format("Request URL: {0}", url));
                string logbody = (item.payload == null ? "null" : (string)item.payload.ToString());
                logger.LogInformation(string.Format("Request BODY: {0}", logbody));
#endif

                throw err;
            }
            catch (WebException err)
            {
                string ErrorRepsonseBody = LogWebException(err);
                if (item.payload == null)
                {
                    logger.LogError("WebException. Payload was null");
                }
                else
                {
                    logger.LogError("WebException. Payload was: " + (string)(item.payload.ToString()));
                }

                //RAB: There are some odd transient errors relating to small buffers too small, gateways etc.
                //Googling suggests that these could be some kind of fault in the class libraries,
                //but also suggets that immediately retrying also succeeds. Here we return the output of a
                //repeat attempt, although there will be no more attempts.
                if (WithRetry == true)
                {
                    logger.LogDebug("A WebException occured, retrying once");
                    return await executeSingleAsync(item, false);
                }

                try
                {
                    var httpResponse = err.Response;
                    if (httpResponse != null)
                    {

                        if (IsValidJson(ErrorRepsonseBody))
                        {
                            dynamic jres = JToken.Parse(ErrorRepsonseBody);
                            int Code = jres.error.code;
                            string Message = jres.error.message.value;
                            logger.LogError(err, string.Format("Service Layer Web Exception (SL: Code {0}, Message '{1}') - Full Service Layer Response: ", Code, Message));
                            throw new ServiceLayerException(err, (int)jres.error.code, (string)jres.error.message.value);
                        }

                        //the body wasn't parsable, just log the raw body
                        logger.LogError(err, "Service Layer Web Exception - Service Layer Response: " + ErrorRepsonseBody);

                    }
                    else
                    {
                        logger.LogError(err, "Service Layer Web Exception - Full Service Layer Response: {{No Response}}");
                    }
                }
                catch (ServiceLayerException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception when trying to grab web error body: " + ex.Message);
                    logger.LogError(err, "Service Layer Web Exception - Unable to retrieve Full Service Layer Response");
                }

                throw;
            }
            catch (Exception err)
            {
                logger.LogError(err, "Service Layer Error: " + err.ToString());
                throw;
            }
        }
        public async Task<bool> executeChangesetAsync(List<BatchInstruction> items, bool WithRetry = true, int OverrideRequestTimeoutSeconds = 0)
        {
            if (items == null || !items.Any())
            {
                logger.LogDebug("executeChangeset(): Empty Batch");
                //alwys returns true unless the batch was empty - errors in protocol are handled via exceptions
                return false;
            }

            //we cannot post a changeset that has a mixture of PATCH commands with different ReplaceCollectionsOnPatch settings 
            if (items.Count() > 1 && items.Any(i => i.ReplaceCollectionsOnPatch && i.method == "PATCH") && items.Any(i => !i.ReplaceCollectionsOnPatch && i.method == "PATCH"))
            {
                logger.LogError("Cannot post a changeset with a mix of ReplaceCollectionsOnPatch requirements.");
                throw new Exception("Cannot post a Service Layer changeset with a mix of ReplaceCollectionsOnPatch requirements.");
            }

            string url = ServerUrl + "$batch";

            //Create a request and specify the request url.
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(url);

            string boundary = string.Format("OchFramework-{0:D}", System.Guid.NewGuid());
            string changesetboundary = string.Format("OchFramework-changeset-{0:D}", System.Guid.NewGuid());

            //Setup the request - batch requests are multi-part posts
            request.ServerCertificateValidationCallback = delegate { return true; };
            request.Method = "POST";
            request.ContentType = "multipart/mixed;boundary=" + boundary;
            request.ServicePoint.Expect100Continue = false;
            if (RequestTimoutSeconds > 0) { request.Timeout = RequestTimoutSeconds * 1000; }
            if (OverrideRequestTimeoutSeconds > 0) { request.Timeout = OverrideRequestTimeoutSeconds * 1000; }
            //note the Hana loadbalancer has a timeout of 300 seconds - see https://blogs.sap.com/2016/05/25/how-to-avoid-502-proxy-error-when-adding-large-documents-via-service-layer/

            var sl = (new System.Uri(url, UriKind.Absolute)).Host;

            //Set cookies, session id essentially gives us the accesss ability to post to the database.
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(new Cookie("B1SESSION", Connection.SessionId, "", sl));
            request.CookieContainer.Add(new Cookie("ROUTEID", Connection.ROUTEID, "", sl));

            //we checked above that ReplaceCollectionsOnPatch was consistent for all changeset items
            if (items.Any(i => i.ReplaceCollectionsOnPatch)) request.Headers.Add("B1S-ReplaceCollectionsOnPatch", "true");

            var forlog = new System.Text.StringBuilder();

            using (var stream = request.GetRequestStream())
            {
                using (var streamWriter = new StreamWriter(stream))
                {
                    //Write out a boundary and a changeset header
                    streamWriter.Write(string.Format("--{0}\r\n", boundary));
                    streamWriter.Write(string.Format("Content-Type: multipart/mixed;boundary={0}\r\n\r\n", changesetboundary));
                    forlog.Append(string.Format("--{0}\r\n", boundary));
                    forlog.Append(string.Format("Content-Type: multipart/mixed;boundary={0}\r\n\r\n", changesetboundary));

                    int contentid = 1;
                    foreach (var item in items)
                    {
                        streamWriter.Write(string.Format("--{0}\r\n", changesetboundary));
                        forlog.Append(string.Format("--{0}\r\n", changesetboundary));

                        //Write out the sub-request
                        streamWriter.Write("Content-Type: application/http\r\n");
                        streamWriter.Write("Content-Transfer-Encoding: binary\r\n");
                        streamWriter.Write("Content-ID: " + (contentid++) + "\r\n");
                        ////B1S-ReplaceCollectionsOnPatch doesn't look like it works on individual service layer items in a changeset, UpdateManager should check for this
                        //if (item.ReplaceCollectionsOnPatch) streamWriter.Write("B1S-ReplaceCollectionsOnPatch: true\r\n");
                        streamWriter.Write("\r\n");

                        forlog.Append("Content-Type: application/http\r\n");
                        forlog.Append("Content-Transfer-Encoding: binary\r\n");
                        forlog.Append("Content-ID: " + (contentid - 1) + "\r\n");
                        //if (item.ReplaceCollectionsOnPatch) forlog.Append("B1S-ReplaceCollectionsOnPatch: true\r\n");
                        forlog.Append("\r\n");

                        if (item.objectKey != null)
                        {
                            if (typeof(Int32).IsAssignableFrom(item.objectKey.GetType()) || typeof(UInt32).IsAssignableFrom(item.objectKey.GetType()) ||
                                typeof(Int64).IsAssignableFrom(item.objectKey.GetType()) || typeof(UInt64).IsAssignableFrom(item.objectKey.GetType()))
                            {
                                streamWriter.Write(string.Format("{0} {1}{2}({3})\r\n\r\n", item.method, "/", item.objectName, item.objectKey));
                                forlog.Append(string.Format("{0} {1}{2}({3})\r\n\r\n", item.method, "/", item.objectName, item.objectKey));
                            }
                            else
                            {
                                streamWriter.Write(string.Format("{0} {1}{2}('{3}')\r\n\r\n", item.method, "/", item.objectName, item.objectKey));
                                forlog.Append(string.Format("{0} {1}{2}('{3}')\r\n\r\n", item.method, "/", item.objectName, item.objectKey));
                            }
                        }
                        else
                        {
                            streamWriter.Write(string.Format("{0} {1}{2}\r\n\r\n", item.method, "/b1s/v1/", item.objectName));
                            forlog.Append(string.Format("{0} {1}{2}\r\n\r\n", item.method, "/b1s/v1/", item.objectName));
                        }
                        var body = item.payloadtype == BatchInstructionPayloadType.Object ? Newtonsoft.Json.JsonConvert.SerializeObject(item.payload) : item.payload;
                        forlog.Append(body);
                        streamWriter.Write(body);
                        forlog.Append("\r\n");
                        streamWriter.Write("\r\n");
                    }
                    streamWriter.Write(string.Format("--{0}--\r\n", changesetboundary));
                    streamWriter.Write(string.Format("--{0}--\r\n", boundary));
                    forlog.Append(string.Format("--{0}--\r\n", changesetboundary));
                    forlog.Append(string.Format("--{0}--\r\n", boundary));
                    LogSLRequest("POST", url, forlog.ToString());

                    streamWriter.Flush();
                }

                //Collect the result and return true or false to the client to indicate if the post request was a success or not.

                try
                {
                    var httpResponse = (HttpWebResponse) await request.GetResponseAsync();

                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                        LogSLResponse("POST", url, result);

                        var boundaryregex = new System.Text.RegularExpressions.Regex(@"^--(.*)$", System.Text.RegularExpressions.RegexOptions.Multiline);
                        var protocolregex = new System.Text.RegularExpressions.Regex(@"^HTTP/\d+\.\d+\s+(\d{3})\s*(.*?)\s*\r\n", System.Text.RegularExpressions.RegexOptions.Singleline);

                        result = "Content-Type: multipart/mixed;boundary=" + boundaryregex.Match(result).Groups[1].Value.TrimEnd() + "\r\n\r\n" + result;

                        var UTF8 = System.Text.Encoding.UTF8;
                        using (var batmemstr = new MemoryStream(UTF8.GetBytes(result)))
                        {
                            //on success, the result will be emp
                            result = "";

                            //We'll need to parse the response because it's quite complex, we use a Mime Library for this
                            var batchmessage = MimeKit.MimeMessage.Load(batmemstr);

                            //Now we need to check each status to find an error - we expect a single batch with a single changeset in it 
                            if (batchmessage != null)
                            {
                                foreach (var batchbp in batchmessage.BodyParts)
                                {
                                    using (var chnmemstr = new MemoryStream(UTF8.GetBytes(batchbp.ToString())))
                                    {
                                        chnmemstr.Seek(0, SeekOrigin.Begin);

                                        var changesetmessage = MimeKit.MimeMessage.Load(chnmemstr);
                                        var embpart = changesetmessage.BodyParts.First();
                                        {
                                            var embstr = (new System.IO.StreamReader(((MimeKit.MimePart)embpart).Content.Stream)).ReadToEnd();
                                            var protocolstatus = protocolregex.Match(embstr).Groups[1].Value;
                                            var protocolmessage = protocolregex.Match(embstr).Groups[2].Value;
                                            embstr = protocolregex.Replace(embstr, "");

                                            int contentID = 0;
                                            string rawContent = "";
                                            using (var embmemstr = new MemoryStream(UTF8.GetBytes(embstr)))
                                            {
                                                embmemstr.Seek(0, SeekOrigin.Begin);

                                                var partmsg = MimeKit.MimeMessage.Load(embmemstr);

                                                contentID = int.Parse(partmsg.Body.ContentId);
                                                var partbody = (MimeKit.MimePart)partmsg.Body;
                                                if (partbody != null && partbody.Content != null && partbody.Content.Stream != null)
                                                {
                                                    rawContent = (new System.IO.StreamReader(((MimeKit.MimePart)partmsg.Body).Content.Stream)).ReadToEnd();
                                                }
                                                else
                                                {
                                                    rawContent = "";
                                                }
                                            }

                                            Exception jsonException = null;
                                            JToken jsonContent = null;
                                            if (ServiceLayer.Client.IsValidJson(rawContent))
                                            {
                                                try
                                                {
                                                    jsonContent = JToken.Parse(rawContent);
                                                }
                                                catch (Exception ex)
                                                {
                                                    jsonException = ex;
                                                    jsonContent = null;
                                                }
                                            }
                                            if (protocolstatus.Length > 0 && (protocolstatus.Substring(0, 1) == "4" || protocolstatus.Substring(0, 1) == "5"))
                                            {
                                                var instruction = items[contentID - 1];
                                                var message = "Changeset error: Method = " + instruction.method + ", Object = " + instruction.objectName + (!string.IsNullOrWhiteSpace((instruction.objectKey ?? "").ToString()) ? ", Key = " + instruction.objectKey : "") + " : HTTP error = " + protocolstatus + " " + protocolmessage;
                                                int SapCode = 0;
                                                string SapMessage = "";
                                                //This is an error, make a batch error exception
                                                if (jsonContent != null)
                                                {
                                                    //search the parsed response for a SAP-centric error
                                                    try
                                                    {
                                                        if (jsonContent["error"] != null)
                                                        {
                                                            SapMessage = (string)jsonContent["error"]["message"]["value"];
                                                            SapCode = (int)jsonContent["error"]["code"];

                                                            message = "Sap Error: Code = " + SapCode + ", message = " + SapMessage + " : " + message;
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        //don't care - if the above portion crashes, then you don't get the Service Layer error in the error message - caller is welcome to reparse the Json if they know what format it's in
                                                    }
                                                }
                                                var exc = new ChangesetException(message, int.Parse(protocolstatus), protocolmessage, instruction);
                                                exc.HttpStatus = int.Parse(protocolstatus);
                                                exc.HttpStatusMessage = protocolmessage;
                                                exc.RawContent = rawContent;
                                                exc.JsonContent = jsonContent;
                                                exc.SapCode = SapCode;
                                                exc.SapMessage = SapMessage;
                                                throw exc;
                                            }
                                            else
                                            {
                                                //this is fine - extract the content and update the appropriate single instruction
                                                var instruction = items[contentID - 1];

                                                var response = new SBOServiceLayerInstructionResponse();
                                                response.HttpStatus = int.Parse(protocolstatus);
                                                response.HttpStatusMessage = protocolmessage;
                                                response.RawContent = rawContent;
                                                response.JsonContent = jsonContent;
                                                response.JsonException = jsonException;
                                                instruction.Response = response;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        //We only ever return true, with results in the Single instruction objects - an error would have already thrown an exception
                        this.ExpectedExpiryTime = DateTime.Now.AddMinutes(Connection.SessionTimeout);
                        return true;
                    }
                }
                catch (WebException err) when (err.Status == WebExceptionStatus.Timeout)
                {
                    //dont retry if the error is a request timeout - it might still be going through and an arbitrary retry could result in the data being processed twice
#if !LOGSL
                    //the request hasn't been previously logged
                    logger.LogError("executeChangeset(): A WebException occured, request has timed out, throwing the error: " + err.ToString());
                    logger.LogInformation(string.Format("Request URL: {0}", url));
                    logger.LogInformation(string.Format("Request BODY: {0}", forlog.ToString()));
#endif
                    logger.LogInformation("Timeout for request was: " + request.Timeout.ToString() + " milliseconds.");
                    throw err;
                }
                catch (WebException err)
                {
                    LogWebException(err);

                    if (WithRetry == true)
                    {
                        logger.LogDebug("executeChangeset(): A WebException occured, retrying once");
                        return await executeChangesetAsync(items, false);
                    }
                    else
                    {
#if !LOGSL
                        //the request hasn't been previously logged
                        logger.LogError("executeChangeset(): A WebException occured, already retried once, throwing the error: " + err.ToString());
                        logger.LogInformation(string.Format("Request URL: {0}", url));
                        logger.LogInformation(string.Format("Request BODY: {0}", forlog.ToString()));
#endif
                    }

                    throw err;
                }
                catch (Exception err)
                {
                    logger.LogError(err, "Service Layer Error: " + err.ToString());

#if !LOGSL
                    //the request hasn't been previously logged
                    logger.LogError("executeChangeset(): A generic exception occured, already retried once, throwing the error. Request was:");
                    logger.LogInformation(string.Format("URL: {0}", url));
                    logger.LogInformation(string.Format("BODY: {0}", forlog.ToString()));
#endif
                    throw;
                }

            }
        }

        private string LogWebException(WebException err)
        {
            string ErrDetails = "WebException\n";
            string ErrResponse = "";
            ErrDetails += err.Message + "\n";
            logger.LogError(err.Status.ToString());

            if (err.Response != null)
            {
                ErrDetails += "Response type: " + err.Response.ContentType + "\n";

                if (err.Response.Headers != null)
                {
                    foreach (string HKey in err.Response.Headers.AllKeys)
                    {
                        ErrDetails += HKey + ": " + string.Join("; ", err.Response.Headers.GetValues(HKey)) + "\n";
                    }
                }

                using (var reader = new StreamReader(err.Response.GetResponseStream()))
                {
                    ErrResponse = reader.ReadToEnd();
                    ErrDetails += "Response: " + ErrResponse + "\n";
                }
            }

            logger.LogError(ErrDetails);
            return ErrResponse;
        }

        //When we add close methods to this class, clients that are associated with a pool must verify that they
        //are not closing the anonymous control connection
        public void associateWithPool(ConnectionPool P)
        {
            containingConnectionPool = P;
        }
    }

}


using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Security;

namespace SAPB1Commons.ServiceLayer
{
    public class ConnectionPool
    {
        ILogger<ConnectionPool> logger = null;
        IServiceProvider sp = null;

        //some functions to aid debugging
        bool _SilentlySkipSLUpdates = false;

        Dictionary<string, Client> connections = new Dictionary<string, Client>();

        public ConnectionPool(ILogger<ConnectionPool> logger, IServiceProvider sp)
        {
            this.logger = logger;
            this.sp = sp;
        }
        public Client GetConnection(string url, string CompanyDB, string UserName, string Password, bool requireauth = false)
        {
            //if we don't specifically require authentication we can try and use a cached session
            if (!requireauth)
            {
                lock (connections) { 
                    if (connections.ContainsKey($"{url},{CompanyDB},{UserName}"))
                    {
                        var existingconnection = connections[$"{url},{CompanyDB},{UserName}"];
                        if (DateTime.UtcNow > existingconnection.Connection.SessionStart.AddMinutes(existingconnection.Connection.SessionTimeout / 2))
                        {
                            logger.LogInformation($"Reconnecting to ServiceLayer User = {url},{CompanyDB},{UserName}");
                            existingconnection.connect();
                        }
                        return existingconnection;
                    }
                }
            }

            //we now need to connect from scratch
            logger.LogInformation($"GetConnection() {url}/{CompanyDB}/{UserName}");

            //dump any previous cached credentials for this user
            lock (connections)
            {
                ClearTimedOutSession(UserName);
                DropConnection(url, CompanyDB, UserName);

                //Create a connection
                logger.LogInformation("Nothing in the SL pool for " + UserName);

                var client = sp.GetService<Client>();
                client.SetConnectionDetails(url, CompanyDB, UserName, Password);
                if (client.connect())
                {
                    logger.LogInformation($"Adding a connection to the SL pool for {url},{CompanyDB},{UserName}");
                    connections.Add($"{url},{CompanyDB},{UserName}", client);

                    return client;
                }
                else
                {
                    throw new ServiceLayerSecurityException("Could not log into Service Layer with users credentials.");
                }
            }
        }

        public Client GetCachedConnection(string url, string CompanyDB, string UserName)
        {
            //if we don't specifically require authentication we can try and use a cached session
            if (connections.ContainsKey($"{url},{CompanyDB},{UserName}"))
            {
                var existingconnection = connections[$"{url},{CompanyDB},{UserName}"];
                if (DateTime.UtcNow > existingconnection.Connection.SessionStart.AddMinutes(existingconnection.Connection.SessionTimeout / 2))
                {
                    logger.LogInformation($"Reconnecting to ServiceLayer User = {url},{CompanyDB},{UserName}");
                    existingconnection.connect();
                }
                return existingconnection;
            }

            return null;
        }

        public bool SilentlySkipSLUpdates
        {
            get { return _SilentlySkipSLUpdates; }
            set { _SilentlySkipSLUpdates = value; }
        }

        public Boolean ClearTimedOutSession(string UserName)
        {

            if (connections.ContainsKey(UserName))
            {
                //We need to make sure that the session isn't timed out
                Connection details = connections.Where(x => x.Key == UserName).First().Value.Connection;
                if(DateTime.UtcNow > details.SessionStart.AddMinutes(details.SessionTimeout / 2))
                {
                    logger.LogInformation($"Reconnecting to ServiceLayer User = {UserName}");
                    //Session has timed out
                    connections.Remove(UserName);
                    return true;
                }
            }

            return false;
        }
        public void DropConnection(string Url, string CompanyDB, string UserName)
        {
            if (connections.ContainsKey($"{Url},{CompanyDB},{UserName}")) connections.Remove($"{Url},{CompanyDB},{UserName}");
            
            //WFHPubSysSessionStore.SessionStoreManager.DeleteSessionStoreEntry<PubSysHanaLib.ServiceLayer.ServiceLayerSession>(UserName, CompanyDB); //safe to call if not present in the session store
        }

        public string CheckLogin(string url, string CompanyDB, string UserName, string Password)
        {
            logger.LogInformation("CheckLogin() " + UserName);

            try
            {
                var client = GetConnection(url, CompanyDB, UserName, Password, true);
                return String.Empty;
            }
            catch (Exception e)
            {
                logger.LogError("CheckLogin() caught error: " + e.Message);
                return e.Message;
            }
        }

        //todo: This refactoring to allow logon by ContextCookie feels a bit awkward and maybe should be reconsidered
        public Client CreateConnectionPoolClientForUserWithPackedCookies(string url, string CompanyDB, string UserName, string PackedCookies)
        {
            if (UserName == null) throw new Exception("UserName not set");

            logger.LogInformation("CreateConnectionPoolClientForUserWithPackedCookies() " + UserName + ", " + url + ", " + CompanyDB);

            //dump the old connection
            DropConnection(url, CompanyDB, UserName);

            var client = sp.GetService<Client>();
            client.SetConnectionDetailsWithCookieString(url, CompanyDB, UserName, PackedCookies);
            //no call to client.Login() is necessary
            connections.Add(UserName, client);
            
            //PubSysHanaLib.ServiceLayer.SessionStoreHelper.CacheUserSession(client);

            return client;
        }
    }
}

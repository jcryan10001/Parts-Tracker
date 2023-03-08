using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.Common;
using System.Data;
using System.Dynamic;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using System;
using PetaPoco.Core;

namespace PetaPoco.Providers.Custom
{
    public class PetaPocoHanaDatabaseNetCoreProvider : DatabaseProvider
    {
        public override DbProviderFactory GetFactory()
        {
           

            //if you have an error buried in GetFactory() this does the same thing:
            //try
            //{
            //    var ft = Type.GetType("Sap.Data.Hana.HanaFactory, Sap.Data.Hana.Core.v2.1, Culture=neutral, PublicKeyToken=0326b8ea63db4bc4");
            //    Console.WriteLine(ft.GetType().Name);

            //    DbProviderFactory pf = (DbProviderFactory)ft.GetField("Instance").GetValue(null);

            //    return pf;
            //}
            //catch (Exception e)
            //{
            //    Logger.Error("GetFactory failed for 'Sap.Data.Hana, Sap.Data.Hana.Core.v2.1'");
            //    Logger.Error(e);
            //    throw e;
            //}

            try
            {
                var factory = GetFactory("Sap.Data.Hana.HanaFactory, Sap.Data.Hana.Core.v2.1, Culture=neutral, PublicKeyToken=0326b8ea63db4bc4");
                return factory;
            }
            catch (Exception e)
            {


                var inner = e.InnerException;
                while (inner != null)
                {
                   
                    inner = inner.InnerException;
                }

                throw e;
            }
        }

        public override string GetParameterPrefix(string connectionString)
        {
            return ":";
        }

        public override string EscapeSqlIdentifier(string sqlIdentifier)
        {
            return string.Format("\"{0}\"", sqlIdentifier);
        }

        public override string GetExistsSql()
        {
            return "SELECT CASE WHEN EXISTS (SELECT 1 FROM {0} WHERE {1}) THEN 1 ELSE 0 END FROM \"DUMMY\"";
        }

        public override object ExecuteInsert(Database database, IDbCommand cmd, string primaryKeyName)
        {
            //todo: untested, based on https://stackoverflow.com/questions/52166632/how-to-get-the-last-inserted-record-identifier-in-sap-hana-database
            if (!cmd.CommandText.TrimEnd().EndsWith(";")) cmd.CommandText += ";";
            cmd.CommandText += "select current_identity_value() FROM DUMMY;";
            return ExecuteScalarHelper(database, cmd);
        }
    }


}

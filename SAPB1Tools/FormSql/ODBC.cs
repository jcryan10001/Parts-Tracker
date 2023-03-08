using System;
using System.Collections.Generic;
using System.Text;

namespace Stratus.Common.SAPB1Tools.FormSql
{
    public class ODBC
    {
        /// <summary>
        /// ?P1 - TableID
        /// ?P2 - AliasID
        /// </summary>
        public static string GetTableUDF()
        {
            return $"SELECT 1 FROM \"CUFD\" WHERE \"TableID\" = ? AND \"AliasID\" = ?";
        }

        /// <summary>
        /// ?P1 - DocNum
        /// </summary>
        /// <param name="Table">The table to query</param>
        /// <returns></returns>
        public static string GetDocEntryFromDocNum(string Table) {
            return $"SELECT \"DocEntry\" FROM {Table} WHERE \"DocNum\" = ?";
        }

        /// <summary>
        /// ?P1 - DocEntry
        /// </summary>
        /// <param name="Table">The table to query</param>
        /// <returns></returns>
        public static string GetDocStatusFromDocEntry(string Table)
        {
            return $"SELECT \"DocStatus\" FROM {Table} WHERE \"DocEntry\" = ?";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SAPB1Commons.ServiceLayer
{
    public class Utils
    {
        public static string IntegerTimeToString(int time)
        {
            return string.Format("{0:00}:{1:00}", time / 100, time % 100);
        }
        public static string EscapeODataField(string orgval)
        {
            if (orgval == null) return null;
            return orgval.Replace("'", "''");
        }
    }
    public static class UtilsExtensions { 
        public static string SafeSubstring(this string value, int startIndex, int? length = null)
        {
            if (length.HasValue)
            {
                return new string((value ?? string.Empty).Skip(startIndex).Take(length.Value).ToArray());
            } else
            {
                return new string((value ?? string.Empty).Skip(startIndex).ToArray());
            }
        }
    }
}

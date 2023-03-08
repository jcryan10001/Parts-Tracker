using System;
using System.Collections.Generic;
using System.Text;
using PetaPoco;

namespace SAPB1Commons.B1Types
{
    public class CUFD
    {
        public string TableID { get; set; }  //SHOULD have leading "@"
        public int FieldID { get; set; }
        public string AliasID { get; set; }  //column name in DB without "U_" prefix
        public string Descr { get; set; }
        public string TypeID { get; set; }
        public int EditSize { get; set; }
        public string Dflt { get; set; }    //default value
        public string NotNull { get; set; } // Y/N

        public string RTable { get; set; }  //The name of a usertable without the leading @ sign
        public string RField { get; set; }

        public List<UFD1> ValidValues { get; set; } = new List<UFD1>();
    }

    public class UFD1
    {
        public string TableID { get; set; }  //SHOULD have leading "@"
        public int FieldID { get; set; }
        public int IndexID { get; set; }
        public string FldValue { get; set; }
        public string Descr { get; set; }
        public DateTime FldDate { get; set; }
    }

}

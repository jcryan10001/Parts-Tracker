using System;
using System.Collections.Generic;
using System.Text;

namespace SAPB1Commons.ServiceLayer
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ServiceLayerFieldName : Attribute
    {
        public string SLName;

        public ServiceLayerFieldName(string Name)
        {
            SLName = Name;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ServiceLayerIgnore : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ServiceLayerReadOnly : Attribute
    {
    }
}

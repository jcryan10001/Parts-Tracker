using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PetaPoco;

namespace PetaPoco.Custom.Mappers
{
    /// <summary>
    ///     Represents an attribute which can decorate a POCO property to mark the property as a column that stores as boolean as a string with values such as 'Y' and 'N'.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class MapYNStringAsBooleanAttribute : Attribute
    {
        public static readonly string TrueValue = "Y";
        public static readonly string FalseValue = "N";
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class MapYesNoStringAsBooleanAttribute : Attribute
    {
        public static readonly string TrueValue = "Yes";
        public static readonly string FalseValue = "No";
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class UDFTimeTypeAttribute : Attribute
    {
        public static readonly string SLFormat = "HH:mm";
    }

    public class PetaPocoB1Mapper : PetaPoco.ConventionMapper
    {
        public override Func<object, object> GetFromDbConverter(System.Reflection.PropertyInfo targetProperty, Type sourceType)
        {
            if (targetProperty != null)
            {
                if (Attribute.IsDefined(targetProperty, typeof(MapYNStringAsBooleanAttribute)))
                {
                    return (x) => (string)x == MapYNStringAsBooleanAttribute.TrueValue;
                }

                if (Attribute.IsDefined(targetProperty, typeof(MapYesNoStringAsBooleanAttribute)))
                {
                    return (x) => (string)x == MapYesNoStringAsBooleanAttribute.TrueValue;
                }

                if (Attribute.IsDefined(targetProperty, typeof(UDFTimeTypeAttribute)))  //these are stored on the database as short integers in hmm format, and stored in memory as string
                {
                    return (x) => ((short?)x).HasValue ? ((short)x / 100).ToString("00") + ":" + ((short)x % 100).ToString("00") : null;
                }
            }

            //if (sourceType == typeof(Sap.Data.Hana.HanaDecimal))
            //{
            //    return (x) => ((Sap.Data.Hana.HanaDecimal)x).ToDecimal();
            //}

            return base.GetFromDbConverter(targetProperty, sourceType);
        }

        public override Func<object, object> GetToDbConverter(System.Reflection.PropertyInfo sourceProperty)
        {
            if (Attribute.IsDefined(sourceProperty, typeof(MapYNStringAsBooleanAttribute)))
            {
                return (x) => ((bool?)x).GetValueOrDefault() ? MapYNStringAsBooleanAttribute.TrueValue : MapYNStringAsBooleanAttribute.FalseValue;
            }

            if (Attribute.IsDefined(sourceProperty, typeof(MapYesNoStringAsBooleanAttribute)))
            {
                return (x) => ((bool?)x).GetValueOrDefault() ? MapYesNoStringAsBooleanAttribute.TrueValue : MapYesNoStringAsBooleanAttribute.FalseValue;
            }

            //don't need a converter for UDFTimeTypeAttribute as Service Layer takes it as a string in HH:mm format
            if (Attribute.IsDefined(sourceProperty, typeof(UDFTimeTypeAttribute)))
            {
                throw new NotImplementedException("Conversion of UDFTimeTypeAttribute properties to database format is not implemented.");
            }

            return base.GetToDbConverter(sourceProperty);
        }

    }
}

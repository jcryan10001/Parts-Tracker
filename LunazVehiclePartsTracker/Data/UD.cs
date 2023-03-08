namespace LunazVehiclePartsTracker.Data
{
    public class UDT
    {

        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public List<UDF> Udf { get; set; }
        public bool Created { get; set; } = false;

    }
    public class UDF
    {

        public string TableName { get; set; }
        public string FieldName { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string SubType { get; set; }
        public int Size { get; set; }
        public string ForeignKey { get; set; }
        public bool Created { get; set; } = false;

    }
    public class SingleUDT
    {

        public string TableName { get; set; }
        public string TableDescription { get; set; }
        public string TableType { get; set; }

    }

}

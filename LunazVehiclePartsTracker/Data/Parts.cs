using Newtonsoft.Json;
using Syncfusion.Blazor.Diagram;
using Syncfusion.Blazor.DropDowns.Internal;
using System.ComponentModel.DataAnnotations;

namespace LunazVehiclePartsTracker.Data
{
    public class Parts 
    {
        public Parts() { }

        public Parts(Parts other)
        {
            Code = other.Code;
            SectionID = other.SectionID;
            AssyRef = other.AssyRef;
            PartNo = other.PartNo;
            Description = other.Description;
            Quantity = other.Quantity;
            Condition = other.Condition;
            Fixing = other.Fixing;
            DryFit = other.DryFit;
            Weight = other.Weight;
            StorageBox = other.StorageBox;
        }

        [JsonProperty(PropertyName = "Code")]
        public string Code { get; set; }

        [JsonProperty(PropertyName = "U_SectionID")]
        public string SectionID { get; set; }

        // Sets column as required and error message to be displayed when empty 
        [Required(ErrorMessage = "This field is required")]
        [JsonProperty(PropertyName = "U_AssyRefID")]
        public string AssyRef { get; set; }

        [JsonProperty(PropertyName = "U_PartNo")]
        public int PartNo { get; set; }

        [JsonProperty(PropertyName = "U_Description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "U_Quantity")]
        public int Quantity { get; set; }

        [JsonProperty(PropertyName = "U_ConditionID")]
        public string Condition { get; set; }

        [JsonProperty(PropertyName = "U_Fixing")]
        public bool Fixing { get; set; }

        [JsonProperty(PropertyName = "U_DryFit")]
        public bool DryFit { get; set; }

        [JsonProperty(PropertyName = "U_Weight")]
        public string Weight { get; set; }

        [JsonProperty(PropertyName = "U_StorageBoxID")]
        public string StorageBox { get; set; }
    }

    public class TreatedParts : ICloneable
    {
        public TreatedParts() { }

        public TreatedParts(TreatedParts other)
        {
            Code = other.Code;
            SectionID = other.SectionID;
            AssyRef = other.AssyRef;
            PartNo = other.PartNo;
            Description = other.Description;
            Quantity = other.Quantity;
            Condition = other.Condition;
            Treatment = other.Treatment;
            TreatmentDescription = other.TreatmentDescription;
            Notes = other.Notes;
            Supplier = other.Supplier;
            Qty = other.Qty;
            PO = other.PO;
            Status = other.Status;
            Index = other.Index;
            NewRecord = other.NewRecord;
        }

        [JsonProperty(PropertyName = "Code")]
        public string Code { get; set; }

        [JsonProperty(PropertyName = "SectionID")]
        public string SectionID { get; set; }
        // Sets column as required and error message to be displayed when empty 

        [JsonProperty(PropertyName = "AssyRef")]
        public string AssyRef { get; set; }

        [JsonProperty(PropertyName = "U_PartID")]
        public int PartNo { get; set; }

        [JsonProperty(PropertyName = "Description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "U_Quantity")]
        public int Quantity { get; set; }

        [JsonProperty(PropertyName = "Condition")]
        public string Condition { get; set; }

        [Required(ErrorMessage = "This field is required")]
        [JsonProperty(PropertyName = "U_TreatmentID")]
        public string Treatment { get; set; }

        [JsonProperty(PropertyName = "U_Description")]
        public string TreatmentDescription { get; set; }

        [JsonProperty(PropertyName = "Qty")]
        public int Qty { get; set; }

        [JsonProperty(PropertyName = "U_Notes")]
        public string Notes { get; set; }

        [JsonProperty(PropertyName = "U_Supplier")]
        public string Supplier { get; set; }

        [JsonProperty(PropertyName = "U_PO")]
        public string PO { get; set; }

        [Required(ErrorMessage = "This field is required")]
        [JsonProperty(PropertyName = "U_Status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "U_TreatmentOrder")]
        public int Index { get; set; }

        [JsonProperty(PropertyName = "NewRecord")]
        public bool NewRecord { get; set; }

        public object Clone()
        {
            return new TreatedParts(this);
        }
    }

    public class ConnectionDetails
    {
        public string DatabaseName { get; set; }
        public string DBPassword { get; set; }
        public string DBServerName { get; set; }
        public string DBType { get; set; }
        public string DBUserName { get; set; }
        public string DBTenantName { get; set; }
        public string ServiceLayerURL { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

    }
    public partial class Settings
    {
        public class ConnectionDetails
        {
            public string DatabaseName { get; set; }
            public string DBPassword { get; set; }
            public string DBServerName { get; set; }
            public string DBType { get; set; }
            public string DBUserName { get; set; }
            public string DBTenantName { get; set; }
            public string ServiceLayerURL { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
        }
    }
    public class GraphDataPointRaw
    {

        public string item { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

    }

    public class PartsUpdateResult
    {
        [JsonProperty(PropertyName = "AddedItems")]
        public List<Parts> AddedItems { get; set; }

        [JsonProperty(PropertyName = "ChangedItems")]
        public List<Parts> ChangedItems { get; set; }

        [JsonProperty(PropertyName = "DeletedItems")]
        public List<Parts> DeletedItems { get; set; }

        [JsonProperty(PropertyName = "TreatedAddedItems")]
        public List<TreatedParts> TreatedAddedItems { get; set; }

        [JsonProperty(PropertyName = "TreatedChangedItems")]
        public List<TreatedParts> TreatedChangedItems { get; set; }

        [JsonProperty(PropertyName = "TreatedDeletedItems")]
        public List<TreatedParts> TreatedDeletedItems { get; set; }
    }
}

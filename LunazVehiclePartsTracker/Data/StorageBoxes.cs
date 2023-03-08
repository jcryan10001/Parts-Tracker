using Newtonsoft.Json;

namespace LunazVehiclePartsTracker.Data
{
    public class StorageBoxes
    {
        [JsonProperty(PropertyName = "ID")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "Description")]
        public string Description { get; set; }
    }
}

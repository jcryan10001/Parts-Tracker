using Newtonsoft.Json;

namespace LunazVehiclePartsTracker.Data
{

    public class Sections
    {
        [JsonProperty(PropertyName = "ID")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "Description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "Parts")]
        public string Parts { get; set; }

    }
}

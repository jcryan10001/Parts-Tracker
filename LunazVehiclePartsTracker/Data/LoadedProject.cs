using Newtonsoft.Json;

namespace LunazVehiclePartsTracker.Data
{
    public class LoadedProject
    {
        [JsonProperty(PropertyName = "Projects")]
        public List<Projects> Projects { get; set; }

        [JsonProperty(PropertyName = "AssyRef")]
        public List<AssyRef> AssyRef { get; set; }

        [JsonProperty(PropertyName = "Condition")]
        public List<Condition> Condition { get; set; }

        [JsonProperty(PropertyName = "StorageBoxes")]
        public List<StorageBoxes> StorageBoxes { get; set; }

        [JsonProperty(PropertyName = "Sections")]
        public List<Sections> Sections { get; set; }

        [JsonProperty(PropertyName = "Parts")]
        public List<Parts> Parts { get; set; }
    }
}

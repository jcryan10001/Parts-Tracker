using Newtonsoft.Json;

namespace LunazVehiclePartsTracker.Data
{
    public class User
    {
        [JsonProperty(PropertyName = "ID")]
        public string ID { get; set; }

        [JsonProperty(PropertyName = "Username")]
        public string Username { get; set; }
    }
}

using Newtonsoft.Json;

namespace LunazVehiclePartsTracker.Data
{
    public class Rootobject
    {
        [JsonProperty(PropertyName = "LoadedProject")]
        public List<LoadedProject> LoadedProjects { get; set; }
    }


    public class TreeItem
    {
        public string NodeId { get; set; }
        public string NodeText { get; set; }
        public string Icon { get; set; }
        public bool Expanded { get; set; }
        public bool Selected { get; set; }
        public List<TreeItem> Child { get; set; }
    }







}

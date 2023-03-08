using System;
using Newtonsoft.Json.Linq;

namespace SAPB1Commons.ServiceLayer
{
    public class SBOServiceLayerInstructionResponse
    {
        public int HttpStatus { get; set; } = 0;
        public string HttpStatusMessage { get; set; } = "";
        public string RawContent { get; set; } = "";
        public JToken JsonContent { get; set; } = null;
        public Exception JsonException { get; set; } = null;
    }
}

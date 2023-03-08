using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SAPB1Commons.ServiceLayer
{
    public class ChangesetException : WebException
    {
        public BatchInstruction instruction { get; set; }
        public int HttpStatus { get; set; } = 0;
        public string HttpStatusMessage { get; set; } = "";
        public int SapCode { get; set; } = 0;
        public string SapMessage { get; set; } = "";
        public string RawContent { get; set; } = "";
        public JToken JsonContent { get; set; } = null;
        public Exception JsonException { get; set; } = null;

        public ChangesetException(string message, int HttpStatus, string HttpStatusMessage, BatchInstruction instruction) : base(message, WebExceptionStatus.ProtocolError)
        {
            this.instruction = instruction;
        }
    }
}

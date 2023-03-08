using System;

namespace SAPB1Commons.ServiceLayer
{
    public class ServiceLayerException : Exception {

        public int SLCode { get; set; } = 0;

        public ServiceLayerException(Exception inner, int Code, string Message) : base(Message, inner)
        {
            SLCode = Code;
        }
    }

    public class ServiceLayerSecurityException : ServiceLayerException
    {
        public ServiceLayerSecurityException(string Message) : base(null, 401, Message)
        {
        }

        public ServiceLayerSecurityException(Exception inner, int Code, string Message) : base(inner, Code, Message)
        {
        }
    }
}


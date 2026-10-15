using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Framework.Exceptions
{
    [Serializable]
    [DataContract]
    public class MethodResultException : Exception
    {
        [DataMember]
        private string message;
        [DataMember]
        private string detailedMessage;

        public MethodResultException()
        {
        }
        public MethodResultException(string _message, string _detailedMessage)
        {
            message = _message;
            detailedMessage = _detailedMessage;
        }

        public override string Message
        {
            get
            {
                return message;
            }
        }
        public override string ToString()
        {
            return detailedMessage ?? message;
        }
    }

}

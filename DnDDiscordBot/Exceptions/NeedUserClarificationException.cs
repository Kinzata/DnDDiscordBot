using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace DnDDiscordBot.Exceptions
{
    public class NeedUserClarificationException : Exception
    {
        public List<string> ClarificationContext { get; set; }

        public NeedUserClarificationException()
        {
        }

        public NeedUserClarificationException(string message, List<string> context) : base(message)
        {
            ClarificationContext = context;
        }

        public NeedUserClarificationException(string message, List<string> context, Exception innerException) : base(message, innerException)
        {
            ClarificationContext = context;
        }

        protected NeedUserClarificationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

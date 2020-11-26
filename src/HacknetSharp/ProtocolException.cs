using System;

namespace HacknetSharp
{
    public class ProtocolException : Exception
    {
        public ProtocolException(string message) : base(message)
        {
        }

        /*public static ProtocolException FromUnexpectedCommand(Command command)
            => new ProtocolException($"Unexpected command {command} received.");*/
    }
}

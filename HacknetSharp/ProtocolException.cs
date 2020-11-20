using System;

namespace HacknetSharp
{
    public class ProtocolException : Exception
    {
        public ProtocolException(string message) : base(message)
        {
        }

        public static ProtocolException FromUnexpectedCommand(ClientServerCommand command)
            => new ProtocolException($"Unexpected command {command} received.");

        public static ProtocolException FromUnexpectedCommand(ServerClientCommand command)
            => new ProtocolException($"Unexpected command {command} received.");
    }
}

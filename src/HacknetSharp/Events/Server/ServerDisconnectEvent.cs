using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when server is disconnecting a client.
    /// </summary>
    [EventCommand(Command.SC_Disconnect)]
    public class ServerDisconnectEvent : ServerEvent
    {
        /// <summary>
        /// Reason for disconnection.
        /// </summary>
        public string Reason { get; set; } = null!;

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteUtf8String(Reason);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Reason = stream.ReadUtf8String();
        }
    }
}

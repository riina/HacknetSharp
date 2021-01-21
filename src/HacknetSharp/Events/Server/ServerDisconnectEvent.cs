using System.IO;
using Azura;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when server is disconnecting a client.
    /// </summary>
    [EventCommand(Command.SC_Disconnect)]
    [Azura]
    public class ServerDisconnectEvent : ServerEvent
    {
        /// <summary>
        /// Reason for disconnection.
        /// </summary>
        [Azura]
        public string Reason { get; set; } = null!;

        /// <inheritdoc />
        public override void Serialize(Stream stream) => ServerDisconnectEventSerialization.Serialize(this, stream);

        /// <inheritdoc />
        public override Event Deserialize(Stream stream) => ServerDisconnectEventSerialization.Deserialize(stream);
    }
}

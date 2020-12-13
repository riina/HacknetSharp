using System.IO;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent when client is disconnecting from the server.
    /// </summary>
    [EventCommand(Command.CS_Disconnect)]
    public class ClientDisconnectEvent : ClientEvent
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static readonly ClientDisconnectEvent Singleton = new ClientDisconnectEvent();

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
        }
    }
}

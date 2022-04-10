using System.IO;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent when client is disconnecting from the server.
    /// </summary>
    [EventCommand(Command.CS_Disconnect)]
    public class ClientDisconnectEvent : ClientEvent
    {
        /// <inheritdoc />
        public ClientDisconnectEvent()
        {
        }

        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static readonly ClientDisconnectEvent Singleton = new();

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
        }

        /// <inheritdoc />
        public override Event Deserialize(Stream stream) => Singleton;
    }
}

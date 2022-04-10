using Azura;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when server is disconnecting a client.
    /// </summary>
    [EventCommand(Command.SC_Disconnect)]
    [Azura]
    public partial class ServerDisconnectEvent : ServerEvent
    {
        /// <inheritdoc />
        public ServerDisconnectEvent()
        {
        }

        /// <summary>
        /// Reason for disconnection.
        /// </summary>
        [Azura]
        public string Reason { get; set; } = null!;
    }
}

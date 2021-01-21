using System.IO;
using Azura;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Represents text alert output intended for the client.
    /// </summary>
    [EventCommand(Command.SC_Alert)]
    [Azura]
    public partial class AlertEvent : ServerEvent
    {
        /// <summary>
        /// Alert kind.
        /// </summary>
        [Azura]
        public byte AlertKind { get; set; }

        /// <summary>
        /// Alert kind.
        /// </summary>
        public Kind Alert
        {
            get => (Kind)AlertKind;
            set => AlertKind = (byte)value;
        }

        /// <summary>
        /// Alert header.
        /// </summary>
        [Azura]
        public string Header { get; set; } = null!;

        /// <summary>
        /// Alert body.
        /// </summary>
        [Azura]
        public string Body { get; set; } = null!;

        /// <summary>
        /// Alert sender.
        /// </summary>
        [Azura]
        public string? Sender { get; set; }

        /// <summary>
        /// Alert kind.
        /// </summary>
        public enum Kind : byte
        {
            /// <summary>
            /// System alert.
            /// </summary>
            System,

            /// <summary>
            /// Network intrusion alert.
            /// </summary>
            Intrusion,

            /// <summary>
            /// Private message.
            /// </summary>
            UserMessage,

            /// <summary>
            /// Administrator message.
            /// </summary>
            AdminMessage,
        }
    }
}

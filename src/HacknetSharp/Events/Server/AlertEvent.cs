using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Represents text alert output intended for the client.
    /// </summary>
    [EventCommand(Command.SC_Alert)]
    public class AlertEvent : ServerEvent
    {
        /// <summary>
        /// Alert kind.
        /// </summary>
        public Kind AlertKind { get; set; }

        /// <summary>
        /// Alert header.
        /// </summary>
        public string Header { get; set; } = null!;

        /// <summary>
        /// Alert body.
        /// </summary>
        public string Body { get; set; } = null!;

        /// <summary>
        /// Alert sender.
        /// </summary>
        public string? Sender { get; set; }

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteU8((byte)AlertKind);
            stream.WriteUtf8String(Header);
            stream.WriteUtf8String(Body);
            stream.WriteUtf8StringNullable(Sender);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            AlertKind = (Kind)stream.ReadU8();
            Header = stream.ReadUtf8String();
            Body = stream.ReadUtf8String();
            Sender = stream.ReadUtf8StringNullable();
        }

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

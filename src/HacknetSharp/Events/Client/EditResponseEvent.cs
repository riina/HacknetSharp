using System;
using System.IO;
using HacknetSharp.Events.Server;
using Ns;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent when client is done editing text in response to <see cref="EditRequestEvent"/>.
    /// </summary>
    [EventCommand(Command.CS_EditResponse)]
    public class EditResponseEvent : ClientResponseEvent
    {
        /// <inheritdoc />
        public override Guid Operation { get; set; }

        /// <summary>
        /// True if user requests the server use the sent <see cref="Content"/>.
        /// </summary>
        public bool Write { get; set; }

        /// <summary>
        /// Modified content.
        /// </summary>
        public string Content { get; set; } = null!;

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteByte(Write ? (byte)1 : (byte)0);
            stream.WriteUtf8String(Content);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Write = stream.ReadU8() != 0;
            Content = stream.ReadUtf8String();
        }
    }
}

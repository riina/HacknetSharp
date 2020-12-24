using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when requesting editing of text content from a user.
    /// </summary>
    [EventCommand(Command.SC_EditRequest)]
    public class EditRequestEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        public Guid Operation { get; set; }

        /// <summary>
        /// True if the sent content is meant to be read-only.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Text to send.
        /// </summary>
        public string Content { get; set; } = null!;

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteU8(ReadOnly ? 1 : 0);
            stream.WriteUtf8String(Content);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            ReadOnly = stream.ReadU8() != 0;
            Content = stream.ReadUtf8String();
        }
    }
}

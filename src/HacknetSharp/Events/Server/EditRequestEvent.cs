using System;
using System.IO;
using Azura;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when requesting editing of text content from a user.
    /// </summary>
    [EventCommand(Command.SC_EditRequest)]
    [Azura]
    public class EditRequestEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        [Azura]
        public Guid Operation { get; set; }

        /// <summary>
        /// True if the sent content is meant to be read-only.
        /// </summary>
        [Azura]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Text to send.
        /// </summary>
        [Azura]
        public string Content { get; set; } = null!;

        /// <inheritdoc />
        public override void Serialize(Stream stream) => EditRequestEventSerialization.Serialize(this, stream);

        /// <inheritdoc />
        public override Event Deserialize(Stream stream) => EditRequestEventSerialization.Deserialize(stream);
    }
}

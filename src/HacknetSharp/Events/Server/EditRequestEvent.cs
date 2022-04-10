using System;
using Azura;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when requesting editing of text content from a user.
    /// </summary>
    [EventCommand(Command.SC_EditRequest)]
    [Azura]
    public partial class EditRequestEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        public EditRequestEvent()
        {
        }

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
    }
}

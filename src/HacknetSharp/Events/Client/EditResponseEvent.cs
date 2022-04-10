using System;
using Azura;
using HacknetSharp.Events.Server;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent when client is done editing text in response to <see cref="EditRequestEvent"/>.
    /// </summary>
    [EventCommand(Command.CS_EditResponse)]
    [Azura]
    public partial class EditResponseEvent : ClientResponseEvent
    {
        /// <inheritdoc />
        public EditResponseEvent()
        {
        }

        /// <inheritdoc />
        [Azura]
        public override Guid Operation { get; set; }

        /// <summary>
        /// True if user requests the server use the sent <see cref="Content"/>.
        /// </summary>
        [Azura]
        public bool Write { get; set; }

        /// <summary>
        /// Modified content.
        /// </summary>
        [Azura]
        public string Content { get; set; } = null!;
    }
}

using System;
using System.IO;
using Azura;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent when client wants to invoke a shell command on the  server.
    /// </summary>
    [EventCommand(Command.CS_Command)]
    [Azura]
    public partial class CommandEvent : ClientEvent, IOperation
    {
        /// <inheritdoc />
        [Azura]
        public Guid Operation { get; set; }

        /// <summary>
        /// Current console width (for server-side text formatting).
        /// </summary>
        [Azura]
        public int ConWidth { get; set; } = -1;

        /// <summary>
        /// Command text to send.
        /// </summary>
        [Azura]
        public string Text { get; set; } = null!;
    }
}

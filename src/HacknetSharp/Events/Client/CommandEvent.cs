using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent when client wants to invoke a shell command on the  server.
    /// </summary>
    [EventCommand(Command.CS_Command)]
    public class CommandEvent : ClientEvent, IOperation
    {
        /// <inheritdoc />
        public Guid Operation { get; set; }

        /// <summary>
        /// Current console width (for server-side text formatting).
        /// </summary>
        public int ConWidth { get; set; } = -1;

        /// <summary>
        /// Command text to send.
        /// </summary>
        public string Text { get; set; } = null!;

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteS32(ConWidth);
            stream.WriteUtf8String(Text);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            ConWidth = stream.ReadS32();
            Text = stream.ReadUtf8String();
        }
    }
}

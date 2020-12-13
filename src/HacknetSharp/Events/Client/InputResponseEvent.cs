using System;
using System.IO;
using HacknetSharp.Events.Server;
using Ns;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent when user input text in response to <see cref="InputRequestEvent"/>.
    /// </summary>
    [EventCommand(Command.CS_InputResponse)]
    public class InputResponseEvent : ClientResponseEvent
    {
        /// <inheritdoc />
        public override Guid Operation { get; set; }

        /// <summary>
        /// Text to send to server.
        /// </summary>
        public string Input { get; set; } = null!;

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteUtf8String(Input);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Input = stream.ReadUtf8String();
        }
    }
}

using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when requesting input from the user.
    /// </summary>
    [EventCommand(Command.SC_InputRequest)]
    public class InputRequestEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        public Guid Operation { get; set; }

        /// <summary>
        /// If true, indicates a request that user input be hidden as it's entered (e.g. for password input).
        /// </summary>
        public bool Hidden { get; set; }

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteU8(Hidden ? 1 : 0);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Hidden = stream.ReadU8() != 0;
        }
    }
}

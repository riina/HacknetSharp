using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent by client on initial connect in order to properly execute startup / on-connect programs.
    /// </summary>
    [EventCommand(Command.CS_InitialCommand)]
    public class InitialCommandEvent : ClientEvent, IOperation
    {
        /// <inheritdoc />
        public Guid Operation { get; set; }

        /// <summary>
        /// Current console width (for server-side text formatting).
        /// </summary>
        public int ConWidth { get; set; } = -1;

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteS32(ConWidth);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            ConWidth = stream.ReadS32();
        }
    }
}

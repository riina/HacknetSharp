using System;
using System.IO;
using Azura;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent by client on initial connect in order to properly execute startup / on-connect programs.
    /// </summary>
    [EventCommand(Command.CS_InitialCommand)]
    [Azura]
    public class InitialCommandEvent : ClientEvent, IOperation
    {
        /// <inheritdoc />
        [Azura]
        public Guid Operation { get; set; }

        /// <summary>
        /// Current console width (for server-side text formatting).
        /// </summary>
        [Azura]
        public int ConWidth { get; set; } = -1;

        /// <inheritdoc />
        public override void Serialize(Stream stream) => InitialCommandEventSerialization.Serialize(this, stream);

        /// <inheritdoc />
        public override Event Deserialize(Stream stream) => InitialCommandEventSerialization.Deserialize(stream);
    }
}

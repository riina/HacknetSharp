using System;
using System.IO;
using Azura;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when requesting input from the user.
    /// </summary>
    [EventCommand(Command.SC_InputRequest)]
    [Azura]
    public class InputRequestEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        [Azura]
        public Guid Operation { get; set; }

        /// <summary>
        /// If true, indicates a request that user input be hidden as it's entered (e.g. for password input).
        /// </summary>
        [Azura]
        public bool Hidden { get; set; }

        /// <inheritdoc />
        public override void Serialize(Stream stream) => InputRequestEventSerialization.Serialize(this, stream);

        /// <inheritdoc />
        public override Event Deserialize(Stream stream) => InputRequestEventSerialization.Deserialize(stream);
    }
}

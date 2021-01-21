using System;
using System.IO;
using Azura;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when the server has successfully completed a user-initiated operation.
    /// </summary>
    [EventCommand(Command.SC_OperationComplete)]
    [Azura]
    public class OperationCompleteEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        [Azura]
        public Guid Operation { get; set; }

        /// <inheritdoc />
        public override void Serialize(Stream stream) => OperationCompleteEventSerialization.Serialize(this, stream);

        /// <inheritdoc />
        public override Event Deserialize(Stream stream) => OperationCompleteEventSerialization.Deserialize(stream);
    }
}

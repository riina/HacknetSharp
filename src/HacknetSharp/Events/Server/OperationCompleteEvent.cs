using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when the server has successfully completed a user-initiated operation.
    /// </summary>
    [EventCommand(Command.SC_OperationComplete)]
    public class OperationCompleteEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        public Guid Operation { get; set; }

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
        }
    }
}

using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Base type for events sent to indicate an error in response to a client-initiated operation.
    /// </summary>
    [EventCommand(Command.SC_FailBaseServer)]
    public class FailBaseServerEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        public Guid Operation { get; set; }

        /// <summary>
        /// Message associated with the failure.
        /// </summary>
        public string Message { get; set; } = "An unknown error occurred.";

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteUtf8String(Message);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Message = stream.ReadUtf8String();
        }
    }
}

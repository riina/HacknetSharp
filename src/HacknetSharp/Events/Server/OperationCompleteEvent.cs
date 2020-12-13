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

        /// <summary>
        /// User's current system address in the active world.
        /// </summary>
        public uint Address { get; set; }

        /// <summary>
        /// User's current working directory in the active world.
        /// </summary>
        public string? Path { get; set; }

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteU32(Address);
            stream.WriteUtf8StringNullable(Path);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Address = stream.ReadU32();
            Path = stream.ReadUtf8StringNullable();
        }
    }
}

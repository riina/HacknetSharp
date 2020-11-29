using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_OperationComplete)]
    public class OperationCompleteEvent : ServerEvent, IOperation
    {
        public Guid Operation { get; set; }
        public string? Address { get; set; }
        public string? Path { get; set; }

        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteUtf8StringNullable(Address);
            stream.WriteUtf8StringNullable(Path);
        }

        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Address = stream.ReadUtf8StringNullable();
            Path = stream.ReadUtf8StringNullable();
        }
    }
}

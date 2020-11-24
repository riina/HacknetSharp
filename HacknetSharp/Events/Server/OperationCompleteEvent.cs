using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_LoginFail)]
    public class OperationCompleteEvent : ServerEvent, IOperation
    {
        public Guid Operation { get; set; }

        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
        }

        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
        }
    }
}

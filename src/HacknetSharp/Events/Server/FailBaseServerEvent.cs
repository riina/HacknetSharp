using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_FailBaseServer)]
    public class FailBaseServerEvent : ServerEvent, IOperation
    {
        public Guid Operation { get; set; }
        public string Message { get; set; } = "An unknown error occurred.";

        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteUtf8String(Message);
        }

        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Message = stream.ReadUtf8String();
        }
    }
}

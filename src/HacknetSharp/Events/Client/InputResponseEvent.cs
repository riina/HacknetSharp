using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Client
{
    [EventCommand(Command.CS_InputResponse)]
    public class InputResponseEvent : ClientEvent, IOperation
    {
        public Guid Operation { get; set; }

        public string Input { get; set; } = null!;

        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteUtf8String(Input);
        }

        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Input = stream.ReadUtf8String();
        }
    }
}

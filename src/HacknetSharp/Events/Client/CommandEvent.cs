using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Client
{
    [EventCommand(Command.CS_Command)]
    public class CommandEvent : ClientEvent, IOperation
    {
        public Guid Operation { get; set; }

        public string Text { get; set; } = null!;

        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteUtf8String(Text);
        }

        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Text = stream.ReadUtf8String();
        }
    }
}

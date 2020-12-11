using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Client
{
    [EventCommand(Command.CS_EditResponse)]
    public class EditResponseEvent : ClientResponseEvent
    {
        public override Guid Operation { get; set; }

        public bool Write { get; set; }

        public string Content { get; set; } = null!;

        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteByte(Write ? (byte)1 : (byte)0);
            stream.WriteUtf8String(Content);
        }

        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Write = stream.ReadU8() != 0;
            Content = stream.ReadUtf8String();
        }
    }
}

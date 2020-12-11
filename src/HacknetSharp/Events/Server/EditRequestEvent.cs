using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_EditRequest)]
    public class EditRequestEvent : ServerEvent, IOperation
    {
        public Guid Operation { get; set; }
        public bool ReadOnly { get; set; }
        public string Content { get; set; } = null!;

        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteU8((byte)(ReadOnly ? 1 : 0));
            stream.WriteUtf8String(Content);
        }

        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            ReadOnly = stream.ReadU8() != 0;
            Content = stream.ReadUtf8String();
        }
    }
}

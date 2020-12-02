using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_InitialCommandComplete)]
    public class InitialCommandCompleteEvent : OperationCompleteEvent
    {
        public bool NeedsRetry { get; set; }

        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteU32(Address);
            stream.WriteUtf8StringNullable(Path);
            stream.WriteU8((byte)(NeedsRetry ? 1 : 0));
        }

        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Address = stream.ReadU32();
            Path = stream.ReadUtf8StringNullable();
            NeedsRetry = stream.ReadU8() != 0;
        }
    }
}

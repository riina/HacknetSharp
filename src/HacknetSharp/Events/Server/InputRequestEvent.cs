using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_InputRequest)]
    public class InputRequestEvent : ServerEvent, IOperation
    {
        public Guid Operation { get; set; }
        public bool Hidden { get; set; }

        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteU8((byte)(Hidden ? 1 : 0));
        }

        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Hidden = stream.ReadU8() != 0;
        }
    }
}

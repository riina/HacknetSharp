using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Client
{
    [EventCommand(Command.CS_InitialCommand)]
    public class InitialCommandEvent : ClientEvent, IOperation
    {
        public Guid Operation { get; set; }
        public int ConWidth { get; set; } = -1;

        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteS32(ConWidth);
        }

        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            ConWidth = stream.ReadS32();
        }
    }
}

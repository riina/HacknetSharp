using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_UserInfo)]
    public class UserInfoEvent : ServerEvent, IOperation
    {
        public Guid Operation { get; set; }

        public bool Admin { get; set; }

        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteU8(Admin ? (byte)1 : (byte)0);
        }

        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Admin = stream.ReadU8() == 1;
        }
    }
}

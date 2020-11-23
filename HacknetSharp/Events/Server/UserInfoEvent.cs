using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_UserInfo)]
    public class UserInfoEvent : ServerEvent
    {
        public bool Admin { get; set; }

        public override void Serialize(Stream stream)
        {
            stream.WriteU8(Admin ? (byte)1 : (byte)0);
        }

        public override void Deserialize(Stream stream)
        {
            Admin = stream.ReadU8() == 1;
        }
    }
}

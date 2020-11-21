using System.IO;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_UserInfo)]
    public class UserInfoEvent : ServerEvent
    {
        public override void Serialize(Stream stream)
        {
        }

        public override void Deserialize(Stream stream)
        {
        }
    }
}

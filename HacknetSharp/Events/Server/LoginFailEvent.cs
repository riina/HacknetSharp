using System.IO;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_LoginFail)]
    public class LoginFailEvent : ServerEvent
    {
        public static readonly LoginFailEvent Singleton = new LoginFailEvent();

        public override void Serialize(Stream stream)
        {
        }

        public override void Deserialize(Stream stream)
        {
        }
    }
}

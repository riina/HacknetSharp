using System.IO;

namespace HacknetSharp.Events.Server
{
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

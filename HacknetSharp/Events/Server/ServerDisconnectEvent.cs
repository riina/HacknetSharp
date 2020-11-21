using System.IO;

namespace HacknetSharp.Events.Server
{
    public class ServerDisconnectEvent : ServerEvent
    {
        public static readonly ServerDisconnectEvent Singleton = new ServerDisconnectEvent();
        public override void Serialize(Stream stream)
        {
        }

        public override void Deserialize(Stream stream)
        {
        }
    }
}

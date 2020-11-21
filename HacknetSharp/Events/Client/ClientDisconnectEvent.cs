using System.IO;

namespace HacknetSharp.Events.Client
{
    public class ClientDisconnectEvent : ClientEvent
    {
        public static readonly ClientDisconnectEvent Singleton = new ClientDisconnectEvent();
        public override void Serialize(Stream stream)
        {
        }

        public override void Deserialize(Stream stream)
        {
        }
    }
}

using System.IO;

namespace HacknetSharp.Events.Client
{
    [EventCommand(Command.CS_Disconnect)]
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

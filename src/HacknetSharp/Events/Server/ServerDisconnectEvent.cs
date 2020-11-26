using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_Disconnect)]
    public class ServerDisconnectEvent : ServerEvent
    {
        public string Reason { get; set; } = null!;

        public override void Serialize(Stream stream)
        {
            stream.WriteUtf8String(Reason);
        }

        public override void Deserialize(Stream stream)
        {
            Reason = stream.ReadUtf8String();
        }
    }
}

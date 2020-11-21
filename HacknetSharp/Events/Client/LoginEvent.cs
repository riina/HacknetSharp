using System.IO;
using Ns;

namespace HacknetSharp.Events.Client
{
    [EventCommand(Command.CS_Login)]
    public class LoginEvent : ClientEvent
    {
        public string User { get; set; } = null!;
        public string Pass { get; set; } = null!;

        public override void Serialize(Stream stream)
        {
            stream.WriteUtf8String(User);
            stream.WriteUtf8String(Pass);
        }

        public override void Deserialize(Stream stream)
        {
            User = stream.ReadUtf8String();
            Pass = stream.ReadUtf8String();
        }
    }
}

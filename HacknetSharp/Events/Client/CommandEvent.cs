using System.IO;
using Ns;

namespace HacknetSharp.Events.Client
{
    [EventCommand(Command.CS_Command)]
    public class CommandEvent : ClientEvent
    {
        public string Text { get; set; } = null!;

        public override void Serialize(Stream stream)
        {
            stream.WriteUtf8String(Text);
        }

        public override void Deserialize(Stream stream)
        {
            Text = stream.ReadUtf8String();
        }
    }
}

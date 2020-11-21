using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_Output)]
    public class OutputEvent : ServerEvent
    {
        public string Text { get; set; }

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

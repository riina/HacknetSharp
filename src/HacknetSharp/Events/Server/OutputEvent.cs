using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Represents text output intended for some form of console on the client.
    /// </summary>
    [EventCommand(Command.SC_Output)]
    public class OutputEvent : ServerEvent
    {
        /// <summary>
        /// Text meant to be written to client's associated console, if any.
        /// </summary>
        public string Text { get; set; } = null!;

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteUtf8String(Text);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Text = stream.ReadUtf8String();
        }
    }
}

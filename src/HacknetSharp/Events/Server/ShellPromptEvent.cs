using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Represents shell prompt event.
    /// </summary>
    [EventCommand(Command.SC_ShellPrompt)]
    public class ShellPromptEvent : ServerEvent
    {
        /// <summary>
        /// Server IPv4 address (big-endian, highest-order octet = first byte).
        /// </summary>
        public uint Address { get; set; }
        /// <summary>
        /// Current working directory for shell.
        /// </summary>
        public string WorkingDirectory { get; set; } = null!;

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteU32(Address);
            stream.WriteUtf8String(WorkingDirectory);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Address = stream.ReadU32();
            WorkingDirectory = stream.ReadUtf8String();
        }
    }
}

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
        /// True if connected to a target server.
        /// </summary>
        public bool TargetConnected { get; set; }

        /// <summary>
        /// Target server IPv4 address (big-endian, highest-order octet = first byte).
        /// </summary>
        public uint TargetAddress { get; set; }

        /// <summary>
        /// Current working directory for shell.
        /// </summary>
        public string WorkingDirectory { get; set; } = null!;

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteU32(Address);
            stream.WriteU8(TargetConnected ? 1 : 0);
            stream.WriteU32(TargetAddress);
            stream.WriteUtf8String(WorkingDirectory);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Address = stream.ReadU32();
            TargetConnected = stream.ReadU8() != 0;
            TargetAddress = stream.ReadU32();
            WorkingDirectory = stream.ReadUtf8String();
        }
    }
}

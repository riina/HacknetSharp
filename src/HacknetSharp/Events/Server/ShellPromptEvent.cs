using System.IO;
using Azura;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Represents shell prompt event.
    /// </summary>
    [EventCommand(Command.SC_ShellPrompt)]
    [Azura]
    public partial class ShellPromptEvent : ServerEvent
    {
        /// <summary>
        /// Server IPv4 address (big-endian, highest-order octet = first byte).
        /// </summary>
        [Azura]
        public uint Address { get; set; }

        /// <summary>
        /// True if connected to a target server.
        /// </summary>
        [Azura]
        public bool TargetConnected { get; set; }

        /// <summary>
        /// Target server IPv4 address (big-endian, highest-order octet = first byte).
        /// </summary>
        [Azura]
        public uint TargetAddress { get; set; }

        /// <summary>
        /// Current working directory for shell.
        /// </summary>
        [Azura]
        public string WorkingDirectory { get; set; } = null!;
    }
}

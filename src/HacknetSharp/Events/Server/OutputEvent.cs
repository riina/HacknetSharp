using System.IO;
using Azura;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Represents text output intended for some form of console on the client.
    /// </summary>
    [EventCommand(Command.SC_Output)]
    [Azura]
    public partial class OutputEvent : ServerEvent
    {
        /// <summary>
        /// Text meant to be written to client's associated console, if any.
        /// </summary>
        [Azura]
        public string Text { get; set; } = null!;
    }
}

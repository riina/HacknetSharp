using System;
using Azura;
using HacknetSharp.Events.Server;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent when user input text in response to <see cref="InputRequestEvent"/>.
    /// </summary>
    [EventCommand(Command.CS_InputResponse)]
    [Azura]
    public partial class InputResponseEvent : ClientResponseEvent
    {
        /// <inheritdoc />
        public InputResponseEvent()
        {
        }

        /// <inheritdoc />
        [Azura]
        public override Guid Operation { get; set; }

        /// <summary>
        /// Text to send to server.
        /// </summary>
        [Azura]
        public string Input { get; set; } = null!;
    }
}

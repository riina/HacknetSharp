using System;
using Azura;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent by client on initial connect in order to properly execute startup / on-connect programs.
    /// </summary>
    [EventCommand(Command.CS_InitialCommand)]
    [Azura]
    public partial class InitialCommandEvent : ClientEvent, IOperation
    {
        /// <inheritdoc />
        public InitialCommandEvent()
        {
        }

        /// <inheritdoc />
        [Azura]
        public Guid Operation { get; set; }

        /// <summary>
        /// Current console width (for server-side text formatting).
        /// </summary>
        [Azura]
        public int ConWidth { get; set; } = -1;
    }
}

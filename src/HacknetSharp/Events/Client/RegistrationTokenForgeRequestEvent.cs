using System;
using Azura;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Requests a registration token from the server, only intended to be honored for administrator users.
    /// </summary>
    [EventCommand(Command.CS_RegistrationTokenForgeRequest)]
    [Azura]
    public partial class RegistrationTokenForgeRequestEvent : ClientEvent, IOperation
    {
        /// <inheritdoc />
        public RegistrationTokenForgeRequestEvent()
        {
        }

        /// <inheritdoc />
        [Azura]
        public Guid Operation { get; set; }
    }
}

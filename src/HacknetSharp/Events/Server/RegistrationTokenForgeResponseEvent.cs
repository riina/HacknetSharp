using System;
using System.IO;
using Azura;
using HacknetSharp.Events.Client;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when successfully generated a token in response to <see cref="RegistrationTokenForgeRequestEvent"/>.
    /// </summary>
    [EventCommand(Command.SC_RegistrationTokenForgeResponse)]
    [Azura]
    public partial class RegistrationTokenForgeResponseEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        [Azura]
        public Guid Operation { get; set; }

        /// <summary>
        /// Create an instance of <see cref="RegistrationTokenForgeResponseEvent"/> with no token. Only meant to be used by event deserializer.
        /// </summary>
        public RegistrationTokenForgeResponseEvent()
        {
            RegistrationToken = null!;
        }

        /// <summary>
        /// Creates a new instance of <see cref="RegistrationTokenForgeRequestEvent"/>.
        /// </summary>
        /// <param name="operation">Operation this event is in response to.</param>
        /// <param name="registrationToken">Registration token.</param>
        public RegistrationTokenForgeResponseEvent(Guid operation, string registrationToken)
        {
            Operation = operation;
            RegistrationToken = registrationToken;
        }

        /// <summary>
        /// Generated registration token.
        /// </summary>
        [Azura]
        public string RegistrationToken { get; set; }
    }
}

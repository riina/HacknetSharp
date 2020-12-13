using System;
using System.IO;
using HacknetSharp.Events.Client;
using Ns;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when successfully generated a token in response to <see cref="RegistrationTokenForgeRequestEvent"/>.
    /// </summary>
    [EventCommand(Command.SC_RegistrationTokenForgeResponse)]
    public class RegistrationTokenForgeResponseEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
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
        public string RegistrationToken { get; set; }

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteUtf8String(RegistrationToken);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            RegistrationToken = stream.ReadUtf8String();
        }
    }
}

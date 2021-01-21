using System;
using System.IO;
using Azura;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Requests a registration token from the server, only intended to be honored for administrator users.
    /// </summary>
    [EventCommand(Command.CS_RegistrationTokenForgeRequest)]
    [Azura]
    public class RegistrationTokenForgeRequestEvent : ClientEvent, IOperation
    {
        /// <inheritdoc />
        [Azura]
        public Guid Operation { get; set; }

        /// <inheritdoc />
        public override void Serialize(Stream stream) =>
            RegistrationTokenForgeRequestEventSerialization.Serialize(this, stream);

        /// <inheritdoc />
        public override Event Deserialize(Stream stream) =>
            RegistrationTokenForgeRequestEventSerialization.Deserialize(stream);
    }
}

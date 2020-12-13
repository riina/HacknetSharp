using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Requests a registration token from the server, only intended to be honored for administrator users.
    /// </summary>
    [EventCommand(Command.CS_RegistrationTokenForgeRequest)]
    public class RegistrationTokenForgeRequestEvent : ClientEvent, IOperation
    {
        /// <inheritdoc />
        public Guid Operation { get; set; }

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
        }
    }
}

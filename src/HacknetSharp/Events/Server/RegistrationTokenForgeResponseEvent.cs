using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_RegistrationTokenForgeResponse)]
    public class RegistrationTokenForgeResponseEvent : ServerEvent, IOperation
    {
        public Guid Operation { get; set; }

        public RegistrationTokenForgeResponseEvent()
        {
            RegistrationToken = null!;
        }

        public RegistrationTokenForgeResponseEvent(Guid operation, string registrationToken)
        {
            Operation = operation;
            RegistrationToken = registrationToken;
        }

        public string RegistrationToken { get; set; }

        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteUtf8String(RegistrationToken);
        }

        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            RegistrationToken = stream.ReadUtf8String();
        }
    }
}

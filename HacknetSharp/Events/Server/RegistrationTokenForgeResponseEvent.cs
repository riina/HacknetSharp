using System.IO;

namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_RegistrationTokenForgeResponse)]
    public class RegistrationTokenForgeResponseEvent : ServerEvent
    {
        public RegistrationTokenForgeResponseEvent()
        {
            RegistrationToken = null!;
        }

        public RegistrationTokenForgeResponseEvent(string registrationToken)
        {
            RegistrationToken = registrationToken;
        }

        public string RegistrationToken { get; set; }

        public override void Serialize(Stream stream)
        {
        }

        public override void Deserialize(Stream stream)
        {
        }
    }
}

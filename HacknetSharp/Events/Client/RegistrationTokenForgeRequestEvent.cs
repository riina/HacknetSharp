using System;
using System.IO;

namespace HacknetSharp.Events.Client
{
    [EventCommand(Command.CS_RegistrationTokenForgeRequest)]
    public class RegistrationTokenForgeRequestEvent : ClientEvent, IOperation
    {
        public override void Serialize(Stream stream)
        {
        }

        public override void Deserialize(Stream stream)
        {
        }

        public Guid Operation { get; set; }
    }
}

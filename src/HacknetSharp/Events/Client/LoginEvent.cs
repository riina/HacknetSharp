using System;
using System.IO;
using Ns;

namespace HacknetSharp.Events.Client
{
    [EventCommand(Command.CS_Login)]
    public class LoginEvent : ClientEvent, IOperation
    {
        public Guid Operation { get; set; }

        public string User { get; set; } = null!;
        public string Pass { get; set; } = null!;
        public string? RegistrationToken { get; set; }

        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteUtf8String(User);
            stream.WriteUtf8String(Pass);
            stream.WriteUtf8StringNullable(RegistrationToken);
        }

        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            User = stream.ReadUtf8String();
            Pass = stream.ReadUtf8String();
            RegistrationToken = stream.ReadUtf8StringNullable();
        }
    }
}

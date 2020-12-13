using System;
using System.IO;
using HacknetSharp.Events.Server;
using Ns;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent when requesting authentication from a server,
    /// triggers either <see cref="LoginFailEvent"/> or <see cref="UserInfoEvent"/> depending on success.
    /// </summary>
    [EventCommand(Command.CS_Login)]
    public class LoginEvent : ClientEvent, IOperation
    {
        /// <inheritdoc />
        public Guid Operation { get; set; }

        /// <summary>
        /// Username to send.
        /// </summary>
        public string User { get; set; } = null!;

        /// <summary>
        /// Password to send.
        /// </summary>
        public string Pass { get; set; } = null!;

        /// <summary>
        /// Registration token to send, if registering user.
        /// </summary>
        public string? RegistrationToken { get; set; }

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteUtf8String(User);
            stream.WriteUtf8String(Pass);
            stream.WriteUtf8StringNullable(RegistrationToken);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            User = stream.ReadUtf8String();
            Pass = stream.ReadUtf8String();
            RegistrationToken = stream.ReadUtf8StringNullable();
        }
    }
}

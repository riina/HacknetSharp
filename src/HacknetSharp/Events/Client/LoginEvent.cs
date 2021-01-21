using System;
using System.IO;
using Azura;
using HacknetSharp.Events.Server;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent when requesting authentication from a server,
    /// triggers either <see cref="LoginFailEvent"/> or <see cref="UserInfoEvent"/> depending on success.
    /// </summary>
    [EventCommand(Command.CS_Login)]
    [Azura]
    public partial class LoginEvent : ClientEvent, IOperation
    {
        /// <inheritdoc />
        [Azura]
        public Guid Operation { get; set; }

        /// <summary>
        /// Username to send.
        /// </summary>
        [Azura]
        public string User { get; set; } = null!;

        /// <summary>
        /// Password to send.
        /// </summary>
        [Azura]
        public string Pass { get; set; } = null!;

        /// <summary>
        /// Registration token to send, if registering user.
        /// </summary>
        [Azura]
        public string? RegistrationToken { get; set; }
    }
}

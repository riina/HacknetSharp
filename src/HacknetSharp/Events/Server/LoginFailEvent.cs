using System;
using System.IO;
using Azura;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when server has failed to authenticate a user.
    /// </summary>
    [EventCommand(Command.SC_LoginFail)]
    [Azura]
    public partial class LoginFailEvent : FailBaseServerEvent
    {
        /// <inheritdoc />
        [Azura]
        public override Guid Operation { get; set; }

        /// <inheritdoc />
        [Azura]
        public override string Message { get; set; } = "Login failed. Invalid credentials.";
    }
}

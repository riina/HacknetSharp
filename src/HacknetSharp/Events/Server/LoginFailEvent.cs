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
    public class LoginFailEvent : FailBaseServerEvent
    {
        /// <inheritdoc />
        [Azura]
        public override Guid Operation { get; set; }

        /// <inheritdoc />
        [Azura]
        public override string Message { get; set; } = "Login failed. Invalid credentials.";

        /// <inheritdoc />
        public override void Serialize(Stream stream) => LoginFailEventSerialization.Serialize(this, stream);

        /// <inheritdoc />
        public override Event Deserialize(Stream stream) => LoginFailEventSerialization.Deserialize(stream);
    }
}

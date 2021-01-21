using System;
using System.IO;
using Azura;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when access to a server resource is denied.
    /// </summary>
    [EventCommand(Command.SC_AccessFail)]
    [Azura]
    public class AccessFailEvent : FailBaseServerEvent
    {
        /// <inheritdoc />
        [Azura]
        public override Guid Operation { get; set; }

        /// <inheritdoc />
        [Azura]
        public override string Message { get; set; } = "Access denied.";

        /// <inheritdoc />
        public override void Serialize(Stream stream) => AccessFailEventSerialization.Serialize(this, stream);

        /// <inheritdoc />
        public override Event Deserialize(Stream stream) => AccessFailEventSerialization.Deserialize(stream);
    }
}

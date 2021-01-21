﻿using System;
using System.IO;
using Azura;
using HacknetSharp.Events.Server;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Event sent when user input text in response to <see cref="InputRequestEvent"/>.
    /// </summary>
    [EventCommand(Command.CS_InputResponse)]
    [Azura]
    public class InputResponseEvent : ClientResponseEvent
    {
        /// <inheritdoc />
        [Azura]
        public override Guid Operation { get; set; }

        /// <summary>
        /// Text to send to server.
        /// </summary>
        [Azura]
        public string Input { get; set; } = null!;

        /// <inheritdoc />
        public override void Serialize(Stream stream) => InputResponseEventSerialization.Serialize(this, stream);

        /// <inheritdoc />
        public override Event Deserialize(Stream stream) => InputResponseEventSerialization.Deserialize(stream);
    }
}

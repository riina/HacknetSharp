using System;
using System.IO;
using Azura;
using HacknetSharp.Events.Client;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when successfully honored <see cref="LoginEvent"/>.
    /// </summary>
    [EventCommand(Command.SC_UserInfo)]
    [Azura]
    public class UserInfoEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        [Azura]
        public Guid Operation { get; set; }

        /// <summary>
        /// True if user is an administrator.
        /// </summary>
        [Azura]
        public bool Admin { get; set; }

        /// <inheritdoc />
        public override void Serialize(Stream stream) => UserInfoEventSerialization.Serialize(this, stream);

        /// <inheritdoc />
        public override Event Deserialize(Stream stream) => UserInfoEventSerialization.Deserialize(stream);
    }
}

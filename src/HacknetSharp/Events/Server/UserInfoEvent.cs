using System;
using System.IO;
using HacknetSharp.Events.Client;
using Ns;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when successfully honored <see cref="LoginEvent"/>.
    /// </summary>
    [EventCommand(Command.SC_UserInfo)]
    public class UserInfoEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        public Guid Operation { get; set; }

        /// <summary>
        /// True if user is an administrator.
        /// </summary>
        public bool Admin { get; set; }

        /// <inheritdoc />
        public override void Serialize(Stream stream)
        {
            stream.WriteGuid(Operation);
            stream.WriteU8(Admin ? (byte)1 : (byte)0);
        }

        /// <inheritdoc />
        public override void Deserialize(Stream stream)
        {
            Operation = stream.ReadGuid();
            Admin = stream.ReadU8() == 1;
        }
    }
}

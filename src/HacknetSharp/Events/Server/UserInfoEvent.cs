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
    public partial class UserInfoEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        [Azura]
        public Guid Operation { get; set; }

        /// <summary>
        /// True if user is an administrator.
        /// </summary>
        [Azura]
        public bool Admin { get; set; }
    }
}

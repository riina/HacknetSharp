using System;
using System.IO;
using Azura;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Base type for events sent to indicate an error in response to a client-initiated operation.
    /// </summary>
    [EventCommand(Command.SC_FailBaseServer)]
    [Azura]
    public partial class FailBaseServerEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        [Azura]
        public virtual Guid Operation { get; set; }

        /// <summary>
        /// Message associated with the failure.
        /// </summary>
        [Azura]
        public virtual string Message { get; set; } = "An unknown error occurred.";
    }
}

﻿using System;
using Azura;

namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when the server has successfully completed a user-initiated operation.
    /// </summary>
    [EventCommand(Command.SC_OperationComplete)]
    [Azura]
    public partial class OperationCompleteEvent : ServerEvent, IOperation
    {
        /// <inheritdoc />
        public OperationCompleteEvent()
        {
        }

        /// <inheritdoc />
        [Azura]
        public Guid Operation { get; set; }
    }
}

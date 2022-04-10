using System;

namespace HacknetSharp.Events.Client
{
    /// <summary>
    /// Base class for client responses to server-initiated operations.
    /// </summary>
    public abstract class ClientResponseEvent : ClientEvent, IOperation
    {
        /// <inheritdoc />
        public ClientResponseEvent()
        {
        }

        /// <inheritdoc />
        public abstract Guid Operation { get; set; }
    }
}

using System;

namespace HacknetSharp.Events.Client
{
    public abstract class ClientResponseEvent: ClientEvent, IOperation
    {
        public abstract Guid Operation { get; set; }
    }
}

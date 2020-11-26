using System;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public interface IPersonContext : IOutboundConnection<ServerEvent>
    {
        PersonModel GetPerson(Guid world);

        public void WriteEventSafe(ServerEvent evt);
    }
}

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using HacknetSharp.Events.Client;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public interface IPersonContext : IOutboundConnection<ServerEvent>
    {
        ConcurrentDictionary<Guid, InputResponseEvent> Inputs { get; }
        PersonModel GetPerson(IWorld world);

        public void WriteEventSafe(ServerEvent evt);
        public Task FlushSafeAsync();
    }
}

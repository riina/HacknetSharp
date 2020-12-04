using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp;
using HacknetSharp.Events.Client;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;

namespace hss.Core
{
    public class AIPersonContext : IPersonContext
    {
        private readonly PersonModel _person;

        public ConcurrentDictionary<Guid, InputResponseEvent> Inputs { get; }

        public AIPersonContext(PersonModel person)
        {
            _person = person;
            Inputs = new ConcurrentDictionary<Guid, InputResponseEvent>();
        }

        public bool Connected => true;

        public void WriteEvent(ServerEvent evt)
        {
        }

        public void WriteEvents(IEnumerable<ServerEvent> events)
        {
        }

        public Task FlushAsync() => Task.CompletedTask;

        public Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public PersonModel GetPerson(IWorld world) => _person;

        public void WriteEventSafe(ServerEvent evt)
        {
        }

        public Task FlushSafeAsync() => Task.CompletedTask;
    }
}

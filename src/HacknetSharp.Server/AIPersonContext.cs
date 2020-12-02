using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp.Server.Common;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server
{
    public class AIPersonContext : IPersonContext
    {
        private readonly PersonModel _person;

        public AIPersonContext(PersonModel person)
        {
            _person = person;
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

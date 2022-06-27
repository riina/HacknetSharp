using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Test.Server;

internal class SynchronousTestServerPersonContext : IPersonContext
{
    public readonly StringBuilder Text;
    private readonly PersonModel _person;

    public SynchronousTestServerPersonContext(PersonModel person)
    {
        _person = person;
        Text = new StringBuilder();
        Responses = new ConcurrentDictionary<Guid, ClientResponseEvent>();
    }

    public bool Connected { get; set; }

    public void WriteEvent(ServerEvent evt)
    {
        if (evt is OutputEvent e) Text.Append(e.Text);
    }

    public void WriteEvents(IEnumerable<ServerEvent> events)
    {
        foreach (var e in events) WriteEvent(e);
    }

    public Task FlushAsync() => Task.CompletedTask;

    public Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public ConcurrentDictionary<Guid, ClientResponseEvent> Responses { get; }
    public PersonModel GetPerson(IWorld world) => _person.World == world.Model ? _person : throw new ArgumentException();

    public void WriteEventSafe(ServerEvent evt) => WriteEvent(evt);

    public Task FlushSafeAsync() => Task.CompletedTask;
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server
{
    public class AIPersonContext : IPersonContext
    {
        private readonly PersonModel _person;

        public ConcurrentDictionary<Guid, ClientResponseEvent> Responses { get; }
        public Queue<Func<string, string>> EditQueue { get; }
        public Queue<string> InputQueue { get; }

        public AIPersonContext(PersonModel person)
        {
            _person = person;
            Responses = new ConcurrentDictionary<Guid, ClientResponseEvent>();
            EditQueue = new Queue<Func<string, string>>();
            InputQueue = new Queue<string>();
        }

        public bool Connected => true;

        public void WriteEvent(ServerEvent evt)
        {
            switch (evt)
            {
                case EditRequestEvent editRequest:
                    EditResponseEvent editResponse;
                    if (EditQueue.TryDequeue(out var edit))
                        editResponse = new EditResponseEvent
                        {
                            Operation = editRequest.Operation, Content = edit(editRequest.Content), Write = true
                        };
                    else
                        editResponse = new EditResponseEvent
                        {
                            Operation = editRequest.Operation, Content = editRequest.Content, Write = false
                        };
                    Responses[editRequest.Operation] = editResponse;
                    break;
                case InputRequestEvent inputRequest:
                    InputResponseEvent inputResponse;
                    if (InputQueue.TryDequeue(out var input))
                        inputResponse = new InputResponseEvent {Operation = inputRequest.Operation, Input = input};
                    else
                        inputResponse = new InputResponseEvent {Operation = inputRequest.Operation, Input = ""};
                    Responses[inputRequest.Operation] = inputResponse;
                    break;
            }
        }

        public void WriteEvents(IEnumerable<ServerEvent> events)
        {
            foreach (var evt in events)
                WriteEvent(evt);
        }

        public Task FlushAsync() => Task.CompletedTask;

        public Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public PersonModel GetPerson(IWorld world) => _person;

        public void WriteEventSafe(ServerEvent evt) => WriteEvent(evt);

        public Task FlushSafeAsync() => Task.CompletedTask;
    }
}

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
    /// <summary>
    /// Represents an NPC player, essentially a wrapper around a <see cref="PersonModel"/> that can be used as a process runner.
    /// </summary>
    public class AIPersonContext : IPersonContext
    {
        private readonly PersonModel _person;

        /// <inheritdoc />
        public ConcurrentDictionary<Guid, ClientResponseEvent> Responses { get; }

        /// <summary>
        /// Queue of edits to make in response to future edit requests.
        /// </summary>
        public Queue<Func<string, string>> EditQueue { get; }

        /// <summary>
        /// Queue of inputs to write in response to future input requests.
        /// </summary>
        public Queue<string> InputQueue { get; }

        /// <summary>
        /// Creates a new instance of <see cref="AIPersonContext"/>
        /// </summary>
        /// <param name="person">Person to wrap.</param>
        public AIPersonContext(PersonModel person)
        {
            _person = person;
            Responses = new ConcurrentDictionary<Guid, ClientResponseEvent>();
            EditQueue = new Queue<Func<string, string>>();
            InputQueue = new Queue<string>();
        }

        /// <inheritdoc />
        public bool Connected => true;

        /// <inheritdoc />
        public void WriteEvent(ServerEvent evt)
        {
            switch (evt)
            {
                case EditRequestEvent editRequest:
                    EditResponseEvent editResponse;
                    if (EditQueue.TryDequeue(out var edit))
                        editResponse = new EditResponseEvent { Operation = editRequest.Operation, Content = edit(editRequest.Content), Write = true };
                    else
                        editResponse = new EditResponseEvent { Operation = editRequest.Operation, Content = editRequest.Content, Write = false };
                    Responses[editRequest.Operation] = editResponse;
                    break;
                case InputRequestEvent inputRequest:
                    InputResponseEvent inputResponse;
                    if (InputQueue.TryDequeue(out var input))
                        inputResponse = new InputResponseEvent { Operation = inputRequest.Operation, Input = input };
                    else
                        inputResponse = new InputResponseEvent { Operation = inputRequest.Operation, Input = "" };
                    Responses[inputRequest.Operation] = inputResponse;
                    break;
            }
        }

        /// <inheritdoc />
        public void WriteEvents(IEnumerable<ServerEvent> events)
        {
            foreach (var evt in events)
                WriteEvent(evt);
        }

        /// <inheritdoc />
        public Task FlushAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <inheritdoc />
        public PersonModel GetPerson(IWorld world) => _person;

        /// <inheritdoc />
        public void WriteEventSafe(ServerEvent evt) => WriteEvent(evt);

        /// <inheritdoc />
        public Task FlushSafeAsync() => Task.CompletedTask;
    }
}

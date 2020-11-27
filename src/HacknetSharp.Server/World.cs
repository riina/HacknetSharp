using System;
using System.Collections.Generic;
using System.Threading;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Common;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server
{
    public class World : IWorld
    {
        public Server Server { get; }
        public WorldModel Model { get; }
        public ISpawn Spawn { get; }
        public IServerDatabase Database { get; }
        public SystemTemplate PlayerSystemTemplate { get; }
        public double Time { get; set; }
        public double PreviousTime { get; set; }
        public HashSet<ProgramOperation> Operations { get; }
        private readonly HashSet<ProgramOperation> _removeOperations;


        internal World(Server server, WorldModel model, IServerDatabase database, Spawn spawn)
        {
            Server = server;
            Model = model;
            Spawn = spawn;
            Database = database;
            PlayerSystemTemplate = server.Templates.SystemTemplates[model.SystemTemplate];
            Operations = new HashSet<ProgramOperation>();
            _removeOperations = new HashSet<ProgramOperation>();
        }

        public void Tick()
        {
            foreach (var operation in Operations)
                if (operation.Update(this))
                {
                    _removeOperations.Add(operation);
                    operation.Context.WriteEventSafe(new OperationCompleteEvent {Operation = operation.OperationId});
                    operation.Context.FlushSafeAsync();
                }

            Operations.ExceptWith(_removeOperations);
        }

        public void ExecuteCommand(IPersonContext person, Guid operationId, string[] args)
        {
            if (args.Length > 0)
            {
                // TODO determine program to use
                /*if (Server.Programs.TryGetValue(line[0].ToLowerInvariant(), out var res))
                {
                    var person = context.GetPerson(this);
                    Operations.Add(new WorldOperation(res.Item1.Invoke(new Common.System(this, person.CurrentSystem)),
                        operationId));
                    return;
                }*/

                person.WriteEventSafe(new OutputEvent {Text = $"{args[0]}: command not found"});
            }

            person.WriteEventSafe(new OperationCompleteEvent {Operation = operationId});
            person.FlushSafeAsync();
        }

        private void ExecuteInternal(IPersonContext context, Guid operationId, string[] line)
        {
        }

        public void RegisterModel<T>(Model<T> model) where T : IEquatable<T>
        {
            Server.RegistrationSet.Add(model);
        }

        public void RegisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>
        {
            Server.RegistrationSet.AddRange(models);
        }

        public void DirtyModel<T>(Model<T> model) where T : IEquatable<T>
        {
            Server.DirtySet.Add(model);
        }

        public void DirtyModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>
        {
            Server.DirtySet.AddRange(models);
        }

        public void DeregisterModel<T>(Model<T> model) where T : IEquatable<T>
        {
            Server.DeregistrationSet.Add(model);
        }

        public void DeregisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>
        {
            Server.DeregistrationSet.AddRange(models);
        }
    }
}

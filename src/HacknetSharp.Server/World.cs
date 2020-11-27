using System;
using System.Collections.Generic;
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
                try
                {
                    if (!operation.Update(this)) continue;
                    _removeOperations.Add(operation);
                    operation.Context.Person.WriteEventSafe(new OperationCompleteEvent
                    {
                        Operation = operation.OperationId
                    });
                    operation.Context.Person.FlushSafeAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            Operations.ExceptWith(_removeOperations);
        }

        public void ExecuteCommand(CommandContext commandContext)
        {
            if (commandContext.Argv.Length > 0)
            {
                var personModel = commandContext.Person.GetPerson(this);
                var system = new Common.System(this, personModel.CurrentSystem);
                commandContext.System = system;
                if (!system.DirectoryExists("/bin"))
                {
                    commandContext.Person.WriteEventSafe(new OutputEvent {Text = "/bin not found"});
                }
                else
                {
                    string exe = $"/bin/{commandContext.Argv[0]}";
                    if (!system.FileExists(exe, true))
                    {
                        commandContext.Person.WriteEventSafe(new OutputEvent
                        {
                            Text = $"{commandContext.Argv[0]}: command not found"
                        });
                    }
                    else
                    {
                        if (Server.Programs.TryGetValue(system.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                            out var res))
                        {
                            Operations.Add(new ProgramOperation(commandContext, res.Item1.Invoke(commandContext)));
                            return;
                        }

                        // If a program with a matching progCode isn't found, just return operation complete.
                    }
                }
            }

            commandContext.Person.WriteEventSafe(new OperationCompleteEvent {Operation = commandContext.OperationId});
            commandContext.Person.FlushSafeAsync();
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

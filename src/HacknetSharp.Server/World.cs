using System;
using System.Collections.Generic;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Common;
using HacknetSharp.Server.Common.Models;
using HacknetSharp.Server.Common.Templates;

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
            PlayerSystemTemplate = server.Templates.SystemTemplates[model.PlayerSystemTemplate];
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
                    if (!operation.Context.PersonContext.Connected) continue;

                    operation.Context.PersonContext.WriteEventSafe(new OperationCompleteEvent
                    {
                        Operation = operation.Context.OperationId,
                        Address = operation.Context.System.Model.Address,
                        Path = operation.Context.Person.WorkingDirectory
                    });
                    if (operation.Context.Disconnect)
                        operation.Context.PersonContext.WriteEventSafe(new ServerDisconnectEvent
                        {
                            Reason = "Disconnected by server."
                        });
                    operation.Context.PersonContext.FlushSafeAsync();
                }
                catch (Exception e)
                {
                    _removeOperations.Add(operation);
                    Console.WriteLine(e);
                }

            Operations.ExceptWith(_removeOperations);
        }

        public void ExecuteCommand(CommandContext commandContext)
        {
            if (commandContext.Argv.Length > 0)
            {
                var personModel = commandContext.Person;
                var system = new Common.System(this, personModel.CurrentSystem);
                commandContext.System = system;
                if (!system.DirectoryExists("/bin"))
                {
                    commandContext.PersonContext.WriteEventSafe(new OutputEvent {Text = "/bin not found"});
                }
                else
                {
                    string exe = $"/bin/{commandContext.Argv[0]}";
                    if (!system.FileExists(exe, true))
                    {
                        commandContext.PersonContext.WriteEventSafe(new OutputEvent
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

            commandContext.PersonContext.WriteEventSafe(new OperationCompleteEvent
            {
                Operation = commandContext.OperationId
            });
            commandContext.PersonContext.FlushSafeAsync();
        }

        public void RegisterModel<T>(Model<T> model) where T : IEquatable<T>
        {
            Server.RegisterModel(model);
        }

        public void RegisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>
        {
            Server.RegisterModels(models);
        }

        public void DirtyModel<T>(Model<T> model) where T : IEquatable<T>
        {
            Server.DirtyModel(model);
        }

        public void DirtyModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>
        {
            Server.DirtyModels(models);
        }

        public void DeregisterModel<T>(Model<T> model) where T : IEquatable<T>
        {
            Server.DeregisterModel(model);
        }

        public void DeregisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>
        {
            Server.DeregisterModels(models);
        }
    }
}

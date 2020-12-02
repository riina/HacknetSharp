using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
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
            PlayerSystemTemplate = server.Templates.SystemTemplates[model.PlayerSystemTemplate.ToLowerInvariant()];
            Operations = new HashSet<ProgramOperation>();
            _removeOperations = new HashSet<ProgramOperation>();
        }

        public void Tick()
        {
            foreach (var operation in Operations)
                try
                {
                    var context = operation.Context;
                    if (!operation.Update(this)) continue;
                    _removeOperations.Add(operation);
                    if (!context.User.Connected) continue;

                    uint addr = context.System.Model.Address;
                    string path = context.Person.WorkingDirectory;
                    context.User.WriteEventSafe(new OperationCompleteEvent
                    {
                        Operation = context.OperationId, Address = addr, Path = path,
                    });
                    if (context.Disconnect)
                        context.User.WriteEventSafe(new ServerDisconnectEvent
                        {
                            Reason = "Disconnected by server."
                        });
                    else
                        context.User.WriteEventSafe(CreatePromptEvent(context.System.Model, context.Person));
                    operation.Context.User.FlushSafeAsync();
                }
                catch (Exception e)
                {
                    _removeOperations.Add(operation);
                    Console.WriteLine(e);
                }

            Operations.ExceptWith(_removeOperations);
        }

        private static OutputEvent CreatePromptEvent(SystemModel system, PersonModel person) =>
            new OutputEvent {Text = $"{UintToAddress(system.Address)}:{person.WorkingDirectory}> "};

        private static string UintToAddress(uint value)
        {
            Span<byte> dst = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(dst, value);
            return $"{dst[0]}.{dst[1]}.{dst[2]}.{dst[3]}";
        }

        public void ExecuteCommand(CommandContext commandContext)
        {
            switch (commandContext.Type)
            {
                case CommandContext.InvocationType.Standard:
                    ExecuteStandardCommand(commandContext);
                    return;
                case CommandContext.InvocationType.Initial:
                    ExecuteInitialCommand(commandContext);
                    return;
                case CommandContext.InvocationType.Boot:
                    // TODO boot program implementation
                    break;
            }

            commandContext.User.WriteEventSafe(new OperationCompleteEvent
            {
                Operation = commandContext.OperationId
            });
            commandContext.User.FlushSafeAsync();
        }

        private void ExecuteStandardCommand(CommandContext commandContext)
        {
            var personModel = commandContext.Person;
            var systemModelKey = personModel.CurrentSystem;
            var systemModel = Model.Systems.FirstOrDefault(x => x.Key == systemModelKey);
            if (systemModel == null)
            {
                // TODO handle missing system
                throw new ApplicationException("Missing system");
            }

            var personModelKey = personModel.Key;
            var loginModel = systemModel.Logins.FirstOrDefault(l => l.Person == personModelKey);
            if (loginModel == null)
            {
                //TODO handle missing login
                throw new ApplicationException("Missing login");
            }

            if (commandContext.Argv.Length > 0)
            {
                var system = new Common.System(this, systemModel);
                commandContext.System = system;
                commandContext.Login = loginModel;
                if (!system.DirectoryExists("/bin"))
                {
                    commandContext.User.WriteEventSafe(new OutputEvent {Text = "/bin not found\n"});
                }
                else
                {
                    string exe = $"/bin/{commandContext.Argv[0]}";
                    if (!system.FileExists(exe, true))
                    {
                        commandContext.User.WriteEventSafe(new OutputEvent
                        {
                            Text = $"{commandContext.Argv[0]}: command not found\n"
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

            commandContext.User.WriteEventSafe(CreatePromptEvent(systemModel, personModel));
            commandContext.User.WriteEventSafe(new OperationCompleteEvent
            {
                Operation = commandContext.OperationId
            });
            commandContext.User.FlushSafeAsync();
        }

        private void ExecuteInitialCommand(CommandContext commandContext)
        {
            var personModel = commandContext.Person;
            var systemModelKey = personModel.CurrentSystem;
            var systemModel = Model.Systems.FirstOrDefault(x => x.Key == systemModelKey);
            if (systemModel == null)
            {
                // TODO handle missing system
                throw new ApplicationException("Missing system");
            }

            var personModelKey = personModel.Key;
            var loginModel = systemModel.Logins.FirstOrDefault(l => l.Person == personModelKey);
            if (loginModel == null)
            {
                //TODO handle missing login
                throw new ApplicationException("Missing login");
            }

            var system = new Common.System(this, systemModel);
            commandContext.System = system;
            commandContext.Login = loginModel;
            commandContext.Argv = Arguments.SplitCommandLine(system.Model.InitialCommandLine ?? "heathcliff");
            if (!system.DirectoryExists("/bin"))
            {
                commandContext.User.WriteEventSafe(new OutputEvent {Text = "/bin not found\n"});
            }
            else
            {
                string exe = $"/bin/{commandContext.Argv[0]}";
                if (!system.FileExists(exe, true))
                {
                    commandContext.User.WriteEventSafe(new OutputEvent
                    {
                        Text = $"{commandContext.Argv[0]}: command not found\n"
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

            // If a program with a matching progCode isn't found, just return operation complete.

            commandContext.User.WriteEventSafe(CreatePromptEvent(systemModel, personModel));
            commandContext.User.WriteEventSafe(new OperationCompleteEvent
            {
                Operation = commandContext.OperationId
            });
            commandContext.User.FlushSafeAsync();
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

﻿using System;
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
        public HashSet<ExecutableOperation> Operations { get; }
        private readonly HashSet<ExecutableOperation> _removeOperations;


        internal World(Server server, WorldModel model, IServerDatabase database, Spawn spawn)
        {
            Server = server;
            Model = model;
            Spawn = spawn;
            Database = database;
            PlayerSystemTemplate = server.Templates.SystemTemplates[model.PlayerSystemTemplate.ToLowerInvariant()];
            Operations = new HashSet<ExecutableOperation>();
            _removeOperations = new HashSet<ExecutableOperation>();
        }

        public void Tick()
        {
            foreach (var operation in Operations)
                try
                {
                    if (!operation.Update(this)) continue;
                    _removeOperations.Add(operation);
                    operation.Complete();
                }
                catch (Exception e)
                {
                    _removeOperations.Add(operation);
                    Console.WriteLine(e);
                }

            Operations.ExceptWith(_removeOperations);
        }

        internal static OutputEvent CreatePromptEvent(SystemModel system, PersonModel person) =>
            new OutputEvent {Text = $"{UintToAddress(system.Address)}:{person.WorkingDirectory}> "};

        private static string UintToAddress(uint value)
        {
            Span<byte> dst = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(dst, value);
            return $"{dst[0]}.{dst[1]}.{dst[2]}.{dst[3]}";
        }

        public void ExecuteCommand(ProgramContext programContext)
        {
            switch (programContext.Type)
            {
                case ProgramContext.InvocationType.Standard:
                    ExecuteStandardCommand(programContext);
                    return;
                case ProgramContext.InvocationType.Initial:
                    ExecuteInitialCommand(programContext);
                    return;
                case ProgramContext.InvocationType.Boot:
                    // TODO boot program implementation
                    break;
            }

            programContext.User.WriteEventSafe(new OperationCompleteEvent {Operation = programContext.OperationId});
            programContext.User.FlushSafeAsync();
        }

        private void ExecuteStandardCommand(ProgramContext programContext)
        {
            var personModel = programContext.Person;
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

            if (programContext.Argv.Length > 0)
            {
                var system = new Common.System(this, systemModel);
                programContext.System = system;
                programContext.Login = loginModel;
                if (!system.DirectoryExists("/bin"))
                {
                    programContext.User.WriteEventSafe(new OutputEvent {Text = "/bin not found\n"});
                }
                else
                {
                    string exe = $"/bin/{programContext.Argv[0]}";
                    if (!system.FileExists(exe, true))
                    {
                        programContext.User.WriteEventSafe(new OutputEvent
                        {
                            Text = $"{programContext.Argv[0]}: command not found\n"
                        });
                    }
                    else
                    {
                        if (Server.Programs.TryGetValue(system.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                            out var res))
                        {
                            Operations.Add(new ProgramOperation(programContext, res.Item1));
                            return;
                        }

                        // If a program with a matching progCode isn't found, just return operation complete.
                    }
                }
            }

            programContext.User.WriteEventSafe(CreatePromptEvent(systemModel, personModel));
            programContext.User.WriteEventSafe(new OperationCompleteEvent {Operation = programContext.OperationId});
            programContext.User.FlushSafeAsync();
        }

        private void ExecuteInitialCommand(ProgramContext programContext)
        {
            var personModel = programContext.Person;
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
            programContext.System = system;
            programContext.Login = loginModel;
            programContext.Argv = Arguments.SplitCommandLine(system.Model.InitialCommandLine ?? "heathcliff");
            if (!system.DirectoryExists("/bin"))
            {
                programContext.User.WriteEventSafe(new OutputEvent {Text = "/bin not found\n"});
            }
            else
            {
                string exe = $"/bin/{programContext.Argv[0]}";
                if (!system.FileExists(exe, true))
                {
                    programContext.User.WriteEventSafe(new OutputEvent
                    {
                        Text = $"{programContext.Argv[0]}: command not found\n"
                    });
                }
                else
                {
                    if (Server.Programs.TryGetValue(system.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                        out var res))
                    {
                        Operations.Add(new ProgramOperation(programContext, res.Item1));
                        return;
                    }

                    // If a program with a matching progCode isn't found, just return operation complete.
                }
            }

            // If a program with a matching progCode isn't found, just return operation complete.

            programContext.User.WriteEventSafe(CreatePromptEvent(systemModel, personModel));
            programContext.User.WriteEventSafe(new OperationCompleteEvent {Operation = programContext.OperationId});
            programContext.User.FlushSafeAsync();
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

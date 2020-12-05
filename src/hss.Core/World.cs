using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using HacknetSharp.Events.Server;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;

namespace hss.Core
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

        public HashSet<Process> Processes { get; }
        private readonly HashSet<Process> _removeProcesses;


        internal World(Server server, WorldModel model, IServerDatabase database, Spawn spawn)
        {
            Server = server;
            Model = model;
            Spawn = spawn;
            Database = database;
            PlayerSystemTemplate = server.Templates.SystemTemplates[model.PlayerSystemTemplate];
            Processes = new HashSet<Process>();
            _removeProcesses = new HashSet<Process>();
        }

        public void Tick()
        {
            foreach (var operation in Processes)
                try
                {
                    // TODO check system and login are still valid, system is up, and user is logged in (non-login -> no output in Complete)
                    if (operation.Context.System.BootTime > Model.Now)
                    {
                        // TODO abrupt process end... writing needs to be done from world
                    }
                    else if (!operation.Update(this)) continue;

                    _removeProcesses.Add(operation);
                }
                catch (Exception e)
                {
                    _removeProcesses.Add(operation);
                    try
                    {
                        operation.Kill();
                    }
                    catch
                    {
                        // ignored
                    }

                    Console.WriteLine(e);
                }

            Processes.ExceptWith(_removeProcesses);
        }

        internal static OutputEvent CreatePromptEvent(SystemModel system, PersonModel person) =>
            new OutputEvent {Text = $"{UintToAddress(system.Address)}:{person.WorkingDirectory}> "};

        private static string UintToAddress(uint value)
        {
            Span<byte> dst = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(dst, value);
            return $"{dst[0]}.{dst[1]}.{dst[2]}.{dst[3]}";
        }

        public void Kill(Process process)
        {
            // TODO implement
            throw new NotImplementedException();
        }

        public void ForceReboot(DateTime rebootTime)
        {
            // TODO implement
            throw new NotImplementedException();
        }

        public void StartShell(PersonModel personModel, SystemModel systemModel, LoginModel loginModel, string line)
        {
            var programContext = new ProgramContext
            {
                World = this,
                Person = personModel,
                User = new AIPersonContext(personModel),
                OperationId = Guid.Empty,
                Argv = Arguments.SplitCommandLine(line),
                Type = ProgramContext.InvocationType.Standard,
                ConWidth = -1,
                System = systemModel,
                Login = loginModel
            };
            var pid = systemModel.GetAvailablePid();
            if (pid == null) return;
            programContext.Pid = pid.Value;
            var process = new ShellProcess(programContext);
            systemModel.Processes.Add(pid.Value, process);
            personModel.ShellChain.Add(process);
        }

        public void StartAICommand(PersonModel personModel, SystemModel systemModel, LoginModel loginModel,
            string line)
        {
            var programContext = new ProgramContext
            {
                World = this,
                Person = personModel,
                User = new AIPersonContext(personModel),
                OperationId = Guid.Empty,
                Argv = Arguments.SplitCommandLine(line),
                Type = ProgramContext.InvocationType.Standard,
                ConWidth = -1,
                System = systemModel,
                Login = loginModel
            };

            if (Server.IntrinsicPrograms.TryGetValue(programContext.Argv[0], out var intrinsicRes))
            {
                Processes.Add(new ProgramProcess(programContext, intrinsicRes.Item1));
                return;
            }

            if (!systemModel.DirectoryExists("/bin")) return;
            string exe = $"/bin/{programContext.Argv[0]}";
            if (!systemModel.FileExists(exe, true)) return;
            var fse = systemModel.GetFileSystemEntry(exe);
            if (fse != null && fse.Kind == FileModel.FileKind.ProgFile &&
                Server.Programs.TryGetValue(systemModel.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                    out var res)) Processes.Add(new ProgramProcess(programContext, res.Item1));
        }

        public void StartDaemon(SystemModel systemModel, string line)
        {
            var serviceContext = new ServiceContext {World = this, Argv = Arguments.SplitCommandLine(line)};
            serviceContext.System = systemModel;
            if (!systemModel.DirectoryExists("/bin")) return;
            string exe = $"/bin/{serviceContext.Argv[0]}";
            if (!systemModel.FileExists(exe, true)) return;
            var fse = systemModel.GetFileSystemEntry(exe);
            if (fse != null && fse.Kind == FileModel.FileKind.ProgFile &&
                Server.Services.TryGetValue(systemModel.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                    out var res)) Processes.Add(new ServiceProcess(serviceContext, res));
        }

        public void ExecuteCommand(ProgramContext programContext)
        {
            var personModel = programContext.Person;

            var personModelKey = personModel.Key;
            if (personModel.ShellChain.Count == 0)
            {
                Console.WriteLine(
                    $"Command tried to execute without an active shell for person {personModelKey} - ignoring request");
                programContext.User.WriteEventSafe(new OperationCompleteEvent {Operation = programContext.OperationId});
                programContext.User.FlushSafeAsync();
                return;
            }

            var shell = personModel.ShellChain[^1];
            var systemModel = shell.Context.System;

            programContext.Argv = programContext.Type switch
            {
                ProgramContext.InvocationType.Connect => systemModel.ConnectCommandLine != null
                    ? Arguments.SplitCommandLine(systemModel.ConnectCommandLine)
                    : Array.Empty<string>(),
                ProgramContext.InvocationType.StartUp => Arguments.SplitCommandLine(Model.StartupCommandLine),
                _ => programContext.Argv
            };

            uint? pid;
            if (programContext.Argv.Length > 0 && (pid = systemModel.GetAvailablePid()).HasValue)
            {
                programContext.System = systemModel;
                programContext.Login = shell.ProgramContext.Login;
                programContext.ParentPid = shell.ProgramContext.Pid;
                programContext.Pid = pid.Value;
                if (Server.IntrinsicPrograms.TryGetValue(programContext.Argv[0], out var intrinsicRes))
                {
                    Processes.Add(new ProgramProcess(programContext, intrinsicRes.Item1));
                    return;
                }

                if (programContext.Type != ProgramContext.InvocationType.StartUp &&
                    !systemModel.DirectoryExists("/bin"))
                {
                    if (programContext.Type == ProgramContext.InvocationType.Standard && !programContext.IsAI)
                        programContext.User.WriteEventSafe(new OutputEvent {Text = "/bin not found\n"});
                }
                else
                {
                    string exe = $"/bin/{programContext.Argv[0]}";
                    if (systemModel.FileExists(exe, true) ||
                        programContext.Type == ProgramContext.InvocationType.StartUp &&
                        systemModel.FileExists(exe, true, true))
                    {
                        var fse = systemModel.GetFileSystemEntry(exe);
                        if (fse != null && fse.Kind == FileModel.FileKind.ProgFile &&
                            Server.Programs.TryGetValue(systemModel.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                                out var res))
                        {
                            Processes.Add(new ProgramProcess(programContext, res.Item1));
                            return;
                        }

                        // If a program with a matching progCode isn't found, just return operation complete.
                    }
                    else
                    {
                        if (programContext.Type == ProgramContext.InvocationType.Standard && !programContext.IsAI)
                            programContext.User.WriteEventSafe(new OutputEvent
                            {
                                Text = $"{programContext.Argv[0]}: command not found\n"
                            });
                    }
                }
            }

            if (!programContext.IsAI)
            {
                programContext.User.WriteEventSafe(CreatePromptEvent(systemModel, personModel));
                programContext.User.WriteEventSafe(new OperationCompleteEvent {Operation = programContext.OperationId});
                programContext.User.FlushSafeAsync();
            }
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

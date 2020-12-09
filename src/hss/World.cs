using System;
using System.Collections.Generic;
using System.Linq;
using HacknetSharp.Events.Server;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;

namespace hss
{
    public class World : IWorld
    {
        public Server Server { get; }
        public WorldModel Model { get; }
        public WorldSpawn Spawn { get; }
        public IServerDatabase Database { get; }
        public SystemTemplate PlayerSystemTemplate { get; }
        public double Time { get; set; }
        public double PreviousTime { get; set; }

        private readonly HashSet<Process> _processes;
        private readonly HashSet<Process> _tmpProcesses;


        private readonly HashSet<ShellProcess> _shellProcesses;
        private readonly HashSet<ShellProcess> _tmpShellProcesses;


        internal World(Server server, WorldModel model, IServerDatabase database)
        {
            Server = server;
            Model = model;
            Spawn = new WorldSpawn(database, Model);
            Database = database;
            PlayerSystemTemplate = server.Templates.SystemTemplates[model.PlayerSystemTemplate];
            _processes = new HashSet<Process>();
            _tmpProcesses = new HashSet<Process>();
            _shellProcesses = new HashSet<ShellProcess>();
            _tmpShellProcesses = new HashSet<ShellProcess>();
        }

        public void Tick()
        {
            TickSet(_processes, _tmpProcesses);
            TickSet(_shellProcesses, _tmpShellProcesses);
        }

        private void TickSet<T>(HashSet<T> processes, HashSet<T> tmpProcesses)
            where T : Process
        {
            tmpProcesses.Clear();
            tmpProcesses.UnionWith(processes);
            foreach (var operation in tmpProcesses)
            {
                try
                {
                    if (!operation.Completed.HasValue)
                    {
                        bool fail = operation.Context is ProgramContext pc0 && !pc0.User.Connected;
                        fail = fail || operation.Context.System.BootTime > Model.Now;
                        fail = fail || !Model.Systems.Contains(operation.Context.System);
                        fail = fail || !operation.Context.System.Logins.Contains(operation.Context.Login);
                        fail = fail || operation.Context is ProgramContext pc2 &&
                            !operation.Context.Person.ShellChain.Contains(pc2.Shell);
                        if (fail)
                        {
                            try
                            {
                                CompleteRecurse(operation, Process.CompletionKind.KillRemote);
                            }
                            catch
                            {
                                // ignored
                            }
                        }
                        else if (!operation.Update(this)) continue;
                    }

                    processes.Remove(operation);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    processes.Remove(operation);
                    try
                    {
                        CompleteRecurse(operation, Process.CompletionKind.KillRemote);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }

        public void CompleteRecurse(Process process, Process.CompletionKind completionKind)
        {
            var context = process.Context;
            var processes = context.System.Processes;
            uint pid = context.Pid;
            bool removed = processes.Remove(pid);
            context.Person.ShellChain.RemoveAll(p => p == process);

            process.Complete(completionKind);
            process.Completed = completionKind;

            if (context is ProgramContext pc)
            {
                var chainLine = pc.ChainLine;
                if (chainLine != null)
                {
                    var genPc = new ProgramContext
                    {
                        World = pc.World,
                        Person = pc.Person,
                        User = pc.User,
                        OperationId = pc.OperationId,
                        Argv = chainLine,
                        Type = ProgramContext.InvocationType.Standard,
                        ConWidth = pc.ConWidth
                    };
                    ExecuteCommand(genPc);
                }
                else
                {
                    uint shellPid = pc.Shell.ProgramContext.Pid;
                    if (removed &&
                        processes.Values.All(p => p.Context.Pid == shellPid || p.Context.ParentPid != shellPid) &&
                        pc.Person.ShellChain.Count != 0)
                    {
                        pc.User.WriteEventSafe(ServerUtil.CreatePromptEvent(pc.Person.ShellChain[^1]));
                        pc.User.FlushSafeAsync();
                    }
                }
            }

            var toKill = processes.Values.Where(p => p.Context.ParentPid == pid).ToList();
            foreach (var p in toKill)
                CompleteRecurse(p, completionKind);
        }

        public void StartShell(IPersonContext personContext, PersonModel personModel, SystemModel systemModel,
            LoginModel loginModel, string line)
        {
            var programContext = new ProgramContext
            {
                World = this,
                Person = personModel,
                User = personContext,
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
            programContext.Shell = process;
            systemModel.Processes.Add(pid.Value, process);
            personModel.ShellChain.Add(process);
            _shellProcesses.Add(process);
        }

        public ProgramProcess? StartProgram(IPersonContext personContext, PersonModel personModel,
            SystemModel systemModel, LoginModel loginModel, string line)
        {
            if (personModel.ShellChain.Count == 0)
                return null;

            var shell = personModel.ShellChain[^1];
            var argv = Arguments.SplitCommandLine(line);

            uint? pid;
            if (argv.Length == 0 || !string.IsNullOrWhiteSpace(argv[0]) ||
                !(pid = systemModel.GetAvailablePid()).HasValue) return null;

            var programContext = new ProgramContext
            {
                World = this,
                Person = personModel,
                User = personContext,
                OperationId = Guid.Empty,
                Argv = argv,
                Type = ProgramContext.InvocationType.Standard,
                ConWidth = -1,
                System = systemModel,
                Login = loginModel,
                ParentPid = shell.ProgramContext.Pid,
                Pid = pid.Value
            };

            if (Server.IntrinsicPrograms.TryGetValue(programContext.Argv[0], out var intrinsicRes))
            {
                var proc = new ProgramProcess(programContext, intrinsicRes.Item1);
                _processes.Add(proc);
                return proc;
            }

            if (!systemModel.DirectoryExists("/bin")) return null;
            string exe = $"/bin/{programContext.Argv[0]}";
            if (!systemModel.FileExists(exe, true)) return null;
            var fse = systemModel.GetFileSystemEntry(exe);
            if (fse == null || fse.Kind != FileModel.FileKind.ProgFile || !TryGetProgramWithHargs(
                systemModel.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                out var res))
                return null;

            {
                programContext.HArgv = res.Item3;
                var proc = new ProgramProcess(programContext, res.Item1);
                _processes.Add(proc);
                return proc;
            }
        }

        public ServiceProcess? StartService(PersonModel personModel, SystemModel systemModel, LoginModel loginModel,
            string line)
        {
            var serviceContext = new ServiceContext
            {
                World = this,
                Argv = Arguments.SplitCommandLine(line),
                System = systemModel,
                Person = personModel,
                Login = loginModel
            };
            if (!systemModel.DirectoryExists("/bin")) return null;
            string exe = $"/bin/{serviceContext.Argv[0]}";
            if (!systemModel.FileExists(exe, true)) return null;
            var fse = systemModel.GetFileSystemEntry(exe);
            if (fse == null || fse.Kind != FileModel.FileKind.ProgFile || !Server.Services.TryGetValue(
                systemModel.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                out var res))
                return null;

            var proc = new ServiceProcess(serviceContext, res);
            _processes.Add(proc);
            return proc;
        }

        public ProgramInfoAttribute? GetProgramInfo(string? content)
        {
            if (content == null) return null;
            var line = Arguments.SplitCommandLine(content);
            if (line.Length == 0 || string.IsNullOrWhiteSpace(line[0])) return null;
            if (Server.IntrinsicPrograms.TryGetValue(line[0], out var prog))
                return prog.Item2;
            if (Server.Programs.TryGetValue(line[0], out prog))
                return prog.Item2;
            return null;
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

            programContext.Shell = shell;
            uint? pid;
            if (programContext.Argv.Length > 0 && !string.IsNullOrWhiteSpace(programContext.Argv[0]) &&
                (pid = systemModel.GetAvailablePid()).HasValue)
            {
                programContext.System = systemModel;
                programContext.Login = shell.ProgramContext.Login;
                programContext.ParentPid = shell.ProgramContext.Pid;
                programContext.Pid = pid.Value;
                if (Server.IntrinsicPrograms.TryGetValue(programContext.Argv[0], out var intrinsicRes))
                {
                    _processes.Add(new ProgramProcess(programContext, intrinsicRes.Item1));
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
                            TryGetProgramWithHargs(systemModel.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                                out var res))
                        {
                            programContext.HArgv = res.Item3;
                            var process = new ProgramProcess(programContext, res.Item1);
                            _processes.Add(process);
                            systemModel.Processes.Add(pid.Value, process);
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
                programContext.User.WriteEventSafe(ServerUtil.CreatePromptEvent(programContext.Shell));
                programContext.User.WriteEventSafe(new OperationCompleteEvent {Operation = programContext.OperationId});
                programContext.User.FlushSafeAsync();
            }
        }

        private bool TryGetProgramWithHargs(string command, out (Program, ProgramInfoAttribute, string[]) result)
        {
            var line = Arguments.SplitCommandLine(command);
            if (line.Length == 0 || string.IsNullOrWhiteSpace(line[0]))
            {
                result = default;
                return false;
            }

            Server.Programs.TryGetValue(line[0], out var res);
            result = (res.Item1, res.Item2, line[1..]);
            return true;
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

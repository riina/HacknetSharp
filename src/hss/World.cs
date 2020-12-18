using System;
using System.Collections.Generic;
using System.Linq;
using HacknetSharp;
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
            if (process.Completed != null) return;
            var context = process.Context;
            var processes = context.System.Processes;
            uint pid = context.Pid;
            processes.Remove(pid);
            if (process is ShellProcess shProc)
            {
                var chain = context.Person.ShellChain;
                int shellIdx = chain.IndexOf(shProc);
                if (shellIdx != -1)
                    chain.RemoveRange(shellIdx, chain.Count - shellIdx);
            }

            process.Complete(completionKind);

            if (context is ProgramContext pc)
            {
                string[]? chainLine = pc.ChainLine;
                if (chainLine != null && completionKind == Process.CompletionKind.Normal)
                {
                    var genPc = ServerUtil.InitTentativeProgramContext(pc.World, pc.OperationId, pc.User, pc.Person,
                        chainLine, conWidth: pc.ConWidth);
                    ExecuteCommand(genPc);
                }
            }

            var toKill = processes.Values.Where(p => p.Context.ParentPid == pid).ToList();
            foreach (var p in toKill)
                CompleteRecurse(p, completionKind);
        }

        public ShellProcess? StartShell(IPersonContext personContext, PersonModel personModel, SystemModel systemModel,
            LoginModel loginModel, string line)
        {
            var programContext =
                ServerUtil.InitProgramContext(this, Guid.Empty, personContext, personModel, loginModel,
                    ServerUtil.SplitCommandLine(line));
            uint? pid = systemModel.GetAvailablePid();
            if (pid == null) return null;
            programContext.Pid = pid.Value;
            var process = new ShellProcess(programContext);
            programContext.Shell = process;
            systemModel.Processes.Add(pid.Value, process);
            var chain = personModel.ShellChain;
            string src = chain.Count != 0 ? Util.UintToAddress(chain[^1].ProgramContext.System.Address) : "<external>";
            double time = Time;
            string logBody = $"User={loginModel.User}\nOrigin={src}\nTime={time}\n";
            chain.Add(process);
            _shellProcesses.Add(process);
            Executable.TryWriteLog(Spawn, time, systemModel, loginModel, ServerConstants.LogKind_Login, logBody, out _);
            return process;
        }

        public ProgramProcess? StartProgram(IPersonContext personContext, PersonModel personModel,
            SystemModel systemModel, LoginModel loginModel, string line)
        {
            if (personModel.ShellChain.Count == 0)
                return null;

            var shell = personModel.ShellChain[^1];
            var argv = ServerUtil.SplitCommandLine(line);

            uint? pid;
            if (argv.Length == 0 || !string.IsNullOrWhiteSpace(argv[0]) ||
                !(pid = systemModel.GetAvailablePid()).HasValue) return null;

            var programContext =
                ServerUtil.InitProgramContext(this, Guid.Empty, personContext, personModel, loginModel, argv);
            programContext.ParentPid = shell.ProgramContext.Pid;
            programContext.Pid = pid.Value;

            if (Server.IntrinsicPrograms.TryGetValue(programContext.Argv[0], out var intrinsicRes))
            {
                var proc = new ProgramProcess(programContext, intrinsicRes.Item1);
                _processes.Add(proc);
                return proc;
            }

            string exe = $"/bin/{programContext.Argv[0]}";
            systemModel.TryGetFile(exe, loginModel, out var result, out var closestStr, out var fse,
                caseInsensitive: true);
            switch (result)
            {
                case ReadAccessResult.NotReadable:
                    personContext.WriteEventSafe(Program.Output($"{closestStr}: Permission denied\n"));
                    personContext.FlushSafeAsync();
                    return null;
                case ReadAccessResult.NoExist:
                    personContext.WriteEventSafe(Program.Output($"{closestStr}: No such file or directory\n"));
                    personContext.FlushSafeAsync();
                    return null;
            }

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
                Argv = ServerUtil.SplitCommandLine(line),
                System = systemModel,
                Person = personModel,
                Login = loginModel
            };
            string exe = $"/bin/{serviceContext.Argv[0]}";
            if (!systemModel.TryGetFile(exe, loginModel, out _, out _, out var fse, caseInsensitive: true))
                return null;
            if (fse.Kind != FileModel.FileKind.ProgFile || !Server.Services.TryGetValue(
                systemModel.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                out var res))
                return null;

            var proc = new ServiceProcess(serviceContext, res);
            _processes.Add(proc);
            return proc;
        }

        public ProgramInfoAttribute? GetProgramInfo(string? argv)
        {
            if (argv == null) return null;
            var line = ServerUtil.SplitCommandLine(argv);
            if (line.Length == 0 || string.IsNullOrWhiteSpace(line[0])) return null;
            if (Server.IntrinsicPrograms.TryGetValue(line[0], out var prog))
                return prog.Item2;
            if (Server.Programs.TryGetValue(line[0], out prog))
                return prog.Item2;
            return null;
        }

        public IEnumerable<(Program, ProgramInfoAttribute)> IntrinsicPrograms => Server.IntrinsicPrograms.Values;

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
                    ? ServerUtil.SplitCommandLine(systemModel.ConnectCommandLine)
                    : Array.Empty<string>(),
                ProgramContext.InvocationType.StartUp => ServerUtil.SplitCommandLine(Model.StartupCommandLine),
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
                    var process = new ProgramProcess(programContext, intrinsicRes.Item1);
                    _processes.Add(process);
                    systemModel.Processes.Add(pid.Value, process);
                    return;
                }

                string exe = $"/bin/{programContext.Argv[0]}";
                systemModel.TryGetFile(exe, programContext.Login, out var result, out var closestStr, out var fse,
                    caseInsensitive: true, hidden: programContext.Type == ProgramContext.InvocationType.StartUp);
                switch (result)
                {
                    case ReadAccessResult.Readable:
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
                        break;
                    case ReadAccessResult.NotReadable:
                        if (programContext.Type == ProgramContext.InvocationType.Standard && !programContext.IsAi)
                            programContext.User.WriteEventSafe(new OutputEvent
                            {
                                Text = $"{closestStr}: permission denied\n"
                            });
                        break;
                    case ReadAccessResult.NoExist:
                        if (programContext.Type == ProgramContext.InvocationType.Standard && !programContext.IsAi)
                            if (closestStr == "/bin")
                                programContext.User.WriteEventSafe(
                                    new OutputEvent {Text = $"{closestStr}: not found\n"});
                            else
                                programContext.User.WriteEventSafe(new OutputEvent
                                {
                                    Text = $"{programContext.Argv[0]}: command not found\n"
                                });
                        break;
                }
            }

            if (!programContext.IsAi)
            {
                programContext.User.WriteEventSafe(ServerUtil.CreatePromptEvent(programContext.Shell));
                programContext.User.WriteEventSafe(new OperationCompleteEvent {Operation = programContext.OperationId});
                programContext.User.FlushSafeAsync();
            }
        }

        private bool TryGetProgramWithHargs(string command, out (Program, ProgramInfoAttribute, string[]) result)
        {
            var line = ServerUtil.SplitCommandLine(command);
            if (line.Length == 0 || string.IsNullOrWhiteSpace(line[0]))
            {
                result = default;
                return false;
            }

            bool success = Server.Programs.TryGetValue(line[0], out var res);
            result = (res.Item1, res.Item2, line);
            return success;
        }
    }
}

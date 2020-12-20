using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        private readonly HashSet<SystemModel> _tmpSystems;


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
            _tmpSystems = new HashSet<SystemModel>();
        }

        public void Tick()
        {
            TickSet(_processes, _tmpProcesses);
            TickSet(_shellProcesses, _tmpShellProcesses);
            // Check processes for memory overflow (shells are static and therefore don't matter)
            TickOverflows(_processes, _tmpProcesses, _tmpSystems);
        }

        private void TickOverflows<T>(HashSet<T> processes, HashSet<Process> tmpProcesses,
            HashSet<SystemModel> tmpSystems) where T : Process
        {
            foreach (var process in processes)
            {
                var system = process.Context.System;
                if (!tmpSystems.Add(system) || system.GetUsedMemory() <= system.SystemMemory) continue;
                tmpProcesses.UnionWith(system.Processes.Values.Where(p => p.Context.ParentPid == 0));
                foreach (var p in tmpProcesses)
                    CompleteRecurse(p, Process.CompletionKind.KillRemote);
                tmpProcesses.Clear();
                system.BootTime = Time + Model.RebootDuration;
            }
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

        public bool TryGetSystem(Guid id, [NotNullWhen(true)] out SystemModel? system)
        {
            //system = Database.GetAsync<Guid, SystemModel>(id).Result;
            system = Model.Systems.FirstOrDefault(f => f.Key == id);
            return system != null;
        }

        public bool TryGetSystem(uint address, [NotNullWhen(true)] out SystemModel? system)
        {
            //system = Database.WhereAsync<SystemModel>(s => s.Address == address).Result.FirstOrDefault();
            system = Model.Systems.FirstOrDefault(f => f.Address == address);
            return system != null;
        }

        public bool CompleteRecurse(Process process, Process.CompletionKind completionKind)
        {
            if (process.Completed != null) return true;
            var context = process.Context;
            var processes = context.System.Processes;
            uint pid = context.Pid;
            processes.Remove(pid);
            if (process is ShellProcess shProc)
            {
                var chain = context.Person.ShellChain;
                int shellIdx = chain.IndexOf(shProc);
                if (shellIdx != -1)
                {
                    int endIdx = chain.Count;
                    for (int i = endIdx - 1; i > shellIdx; i--)
                        CompleteRecurse(chain[i], completionKind);
                    chain.RemoveAt(shellIdx);
                }
            }

            if (!process.Complete(completionKind)) return false;

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
            return true;
        }

        public ShellProcess? StartShell(IPersonContext personContext, PersonModel personModel, SystemModel systemModel,
            LoginModel loginModel, string line)
        {
            var programContext =
                ServerUtil.InitProgramContext(this, Guid.Empty, personContext, personModel, loginModel,
                    ServerUtil.SplitCommandLine(line));
            return StartShell(programContext, Server.IntrinsicPrograms[ServerConstants.ShellName].Item1);
        }

        public ProgramProcess? StartProgram(IPersonContext personContext, PersonModel personModel,
            SystemModel systemModel, LoginModel loginModel, string line)
        {
            if (personModel.ShellChain.Count == 0)
                return null;

            var shell = personModel.ShellChain[^1];
            string[] argv = ServerUtil.SplitCommandLine(line);

            if (argv.Length == 0 || !string.IsNullOrWhiteSpace(argv[0])) return null;

            var programContext =
                ServerUtil.InitProgramContext(this, Guid.Empty, personContext, personModel, loginModel, argv);
            programContext.ParentPid = shell.ProgramContext.Pid;
            programContext.Shell = shell;
            programContext.System = systemModel;

            if (TryGetIntrinsicProgramWithHargs(programContext.Argv[0], out var intrinsicRes))
                return StartProgram(programContext, intrinsicRes.Item1);

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

            programContext.HArgv = res.Item3;
            return StartProgram(programContext, res.Item1);
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
            if (fse.Kind != FileModel.FileKind.ProgFile || !TryGetServiceWithHargs(
                systemModel.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                out var res))
                return null;

            serviceContext.HArgv = res.Item3;
            return StartService(serviceContext, res.Item1);
        }

        private ShellProcess? StartShell(ProgramContext context, Program program)
        {
            var system = context.System;
            var person = context.Person;
            var login = context.Login;
            if (program.GetStartupMemory(context) + context.System.GetUsedMemory() > context.System.SystemMemory)
                return null;
            uint? pid = system.GetAvailablePid();
            if (pid == null) return null;
            context.Pid = pid.Value;

            var process = new ShellProcess(context);
            context.Shell = process;
            system.Processes.Add(pid.Value, process);
            _shellProcesses.Add(process);

            var chain = person.ShellChain;
            string src = chain.Count != 0 ? Util.UintToAddress(chain[^1].ProgramContext.System.Address) : "<external>";
            double time = Time;
            string logBody = $"User={login.User}\nOrigin={src}\nTime={time}\n";
            chain.Add(process);
            Executable.TryWriteLog(Spawn, time, system, login, ServerConstants.LogKind_Login, logBody, out _);
            return process;
        }

        private ProgramProcess? StartProgram(ProgramContext context, Program program)
        {
            if (program.GetStartupMemory(context) + context.System.GetUsedMemory() > context.System.SystemMemory)
                return null;
            var system = context.System;
            uint? pid = system.GetAvailablePid();
            if (pid == null) return null;
            context.Pid = pid.Value;

            var process = new ProgramProcess(context, program);
            system.Processes.Add(pid.Value, process);
            _processes.Add(process);

            return process;
        }

        private ServiceProcess? StartService(ServiceContext context, Service service)
        {
            if (service.GetStartupMemory(context) + context.System.GetUsedMemory() > context.System.SystemMemory)
                return null;
            var system = context.System;
            uint? pid = system.GetAvailablePid();
            if (pid == null) return null;
            context.Pid = pid.Value;

            var process = new ServiceProcess(context, service);
            system.Processes.Add(pid.Value, process);
            _processes.Add(process);

            return process;
        }

        public ProgramInfoAttribute? GetProgramInfo(string? argv)
        {
            if (argv == null) return null;
            string[] line = ServerUtil.SplitCommandLine(argv);
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
                ProgramContext.InvocationType.StartUp => Model.StartupCommandLine != null
                    ? ServerUtil.SplitCommandLine(Model.StartupCommandLine)
                    : Array.Empty<string>(),
                _ => programContext.Argv
            };

            programContext.Shell = shell;
            if (programContext.Argv.Length > 0 && !string.IsNullOrWhiteSpace(programContext.Argv[0]))
            {
                programContext.System = systemModel;
                programContext.Login = shell.ProgramContext.Login;
                programContext.ParentPid = shell.ProgramContext.Pid;
                if (TryGetIntrinsicProgramWithHargs(programContext.Argv[0], out var intrinsicRes))
                {
                    programContext.HArgv = intrinsicRes.Item3;
                    bool success = StartProgram(programContext, intrinsicRes.Item1) != null;
                    if (!success)
                        programContext.User.WriteEventSafe(
                            Program.Output("Process creation failed: out of memory\n"));
                    return;
                }

                string exe = $"/bin/{programContext.Argv[0]}";
                systemModel.TryGetFile(exe, programContext.Login, out var result, out var closestStr, out var fse,
                    caseInsensitive: true,
                    hidden: programContext.Type == ProgramContext.InvocationType.StartUp ? null : false);
                switch (result)
                {
                    case ReadAccessResult.Readable:
                        if (fse != null && fse.Kind == FileModel.FileKind.ProgFile &&
                            TryGetProgramWithHargs(systemModel.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                                out var res))
                        {
                            programContext.HArgv = res.Item3;
                            bool success = StartProgram(programContext, res.Item1) != null;
                            if (!success)
                                programContext.User.WriteEventSafe(
                                    Program.Output("Process creation failed: out of memory\n"));
                            return;
                        }

                        // If a program with a matching progCode isn't found, just return operation complete.
                        break;
                    case ReadAccessResult.NotReadable:
                        if (programContext.Type == ProgramContext.InvocationType.Standard && !programContext.IsAi)
                            programContext.User.WriteEventSafe(
                                Program.Output($"{closestStr}: permission denied\n"));
                        break;
                    case ReadAccessResult.NoExist:
                        if (programContext.Type == ProgramContext.InvocationType.Standard && !programContext.IsAi)
                            if (closestStr == "/bin")
                                programContext.User.WriteEventSafe(Program.Output($"{closestStr}: not found\n"));
                            else
                                programContext.User.WriteEventSafe(
                                    Program.Output($"{programContext.Argv[0]}: command not found\n"));
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

        private bool TryGetIntrinsicProgramWithHargs(string command,
            out (Program, ProgramInfoAttribute, string[]) result)
        {
            string[] line = ServerUtil.SplitCommandLine(command);
            if (line.Length == 0 || string.IsNullOrWhiteSpace(line[0]))
            {
                result = default;
                return false;
            }

            bool success = Server.IntrinsicPrograms.TryGetValue(line[0], out var res);
            result = (res.Item1, res.Item2, line);
            return success;
        }

        private bool TryGetProgramWithHargs(string command, out (Program, ProgramInfoAttribute, string[]) result)
        {
            string[] line = ServerUtil.SplitCommandLine(command);
            if (line.Length == 0 || string.IsNullOrWhiteSpace(line[0]))
            {
                result = default;
                return false;
            }

            bool success = Server.Programs.TryGetValue(line[0], out var res);
            result = (res.Item1, res.Item2, line);
            return success;
        }

        private bool TryGetServiceWithHargs(string command, out (Service, ServiceInfoAttribute, string[]) result)
        {
            string[] line = ServerUtil.SplitCommandLine(command);
            if (line.Length == 0 || string.IsNullOrWhiteSpace(line[0]))
            {
                result = default;
                return false;
            }

            bool success = Server.Services.TryGetValue(line[0], out var res);
            result = (res.Item1, res.Item2, line);
            return success;
        }
    }
}

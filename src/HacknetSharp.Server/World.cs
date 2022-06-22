using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Lua;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;
using Microsoft.Extensions.Logging;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Main world implementation.
    /// </summary>
    public class World : IWorld
    {
        /// <inheritdoc />
        public IReadOnlyCollection<IWorldPlugin> Plugins => _plugins;

        /// <inheritdoc />
        public TemplateGroup Templates { get; }

        /// <inheritdoc />
        public ILogger Logger { get; }

        /// <inheritdoc />
        public WorldModel Model { get; }

        /// <inheritdoc />
        public WorldSpawn Spawn { get; }

        /// <inheritdoc />
        public IServerDatabase Database { get; }

        /// <inheritdoc />
        public SystemTemplate PlayerSystemTemplate { get; }

        /// <inheritdoc />
        public double Time { get; internal set; }

        /// <inheritdoc />
        public double PreviousTime { get; internal set; }

        private readonly ServerBase _server;
        private readonly HashSet<Process> _processes;
        private readonly HashSet<Process> _tmpProcesses;
        private readonly HashSet<ShellProcess> _shellProcesses;
        private readonly HashSet<ShellProcess> _tmpShellProcesses;
        private readonly HashSet<SystemModel> _tmpSystems;
        private readonly HashSet<IWorldPlugin> _plugins;

        /// <summary>
        /// Initializes a new instance of <see cref="World"/>.
        /// </summary>
        /// <param name="server">Server.</param>
        /// <param name="model">World model.</param>
        public World(ServerBase server, WorldModel model)
        {
            _plugins = new HashSet<IWorldPlugin>();
            // TODO need path to automatically derive and instantiate plugins
            CreateAndRegisterPlugin(typeof(ScriptManager));
            Templates = server.Templates;
            _server = server;
            Model = model;
            Database = server.Database;
            Spawn = new WorldSpawn(Database, Model);
            PlayerSystemTemplate = server.Templates.SystemTemplates[model.PlayerSystemTemplate];
            Logger = server.Logger;
            var tmpMissions = new HashSet<MissionModel>();
            foreach (var person in Model.Persons)
            {
                tmpMissions.Clear();
                tmpMissions.UnionWith(person.Missions);
                foreach (var mission in tmpMissions)
                    if (person.Missions.Contains(mission))
                        if (!Templates.MissionTemplates.TryGetValue(mission.Template, out var m))
                            Spawn.RemoveMission(mission);
                        else mission.Data = m;

                Model.ActiveMissions.UnionWith(person.Missions);
                if (person.Tag != null)
                {
                    if (!Model.TaggedPersons.TryGetValue(person.Tag, out var list))
                        Model.TaggedPersons[person.Tag] = list = new List<PersonModel>();
                    list.Add(person);
                }

                if (person.SpawnGroup != Guid.Empty)
                {
                    if (!Model.SpawnGroupPersons.TryGetValue(person.SpawnGroup, out var list))
                        Model.SpawnGroupPersons[person.SpawnGroup] = list = new List<PersonModel>();
                    list.Add(person);
                }
            }

            foreach (var system in Model.Systems)
            {
                Model.AddressedSystems[system.Address] = system;
                if (system.Tag != null)
                {
                    if (!Model.TaggedSystems.TryGetValue(system.Tag, out var list))
                        Model.TaggedSystems[system.Tag] = list = new List<SystemModel>();
                    list.Add(system);
                }

                if (system.SpawnGroup != Guid.Empty)
                {
                    if (!Model.SpawnGroupSystems.TryGetValue(system.SpawnGroup, out var list))
                        Model.SpawnGroupSystems[system.SpawnGroup] = list = new List<SystemModel>();
                    list.Add(system);
                }
            }

            _processes = new HashSet<Process>();
            _tmpProcesses = new HashSet<Process>();
            _shellProcesses = new HashSet<ShellProcess>();
            _tmpShellProcesses = new HashSet<ShellProcess>();
            _tmpSystems = new HashSet<SystemModel>();
        }

        private void CreateAndRegisterPlugin(Type t)
        {
            if (_plugins.Any(v => v.GetType() == t)) throw new ArgumentException("Plugin of specified type is already registered");
            if (Activator.CreateInstance(t) is not IWorldPlugin instance) throw new ArgumentException("Type could not be resolved as plugin");
            instance.Initialize(this);
            _plugins.Add(instance);
        }

        /// <inheritdoc />
        public void Tick()
        {
            TickSet(_processes, _tmpProcesses);
            TickSet(_shellProcesses, _tmpShellProcesses);
            // Check processes for memory overflow (shells are static and therefore don't matter)
            TickOverflows(_processes, _tmpProcesses, _tmpSystems);
            foreach(var plugin in _plugins) plugin.Tick();
        }

        private void TickOverflows<T>(HashSet<T> processes, HashSet<Process> tmpProcesses,
            HashSet<SystemModel> tmpSystems) where T : Process
        {
            tmpSystems.Clear();
            foreach (var process in processes)
            {
                var system = process.ProcessContext.System;
                if (!tmpSystems.Add(system) || system.GetUsedMemory() <= system.SystemMemory) continue;
                Logger.LogInformation("Memory overflowed for system {System}, rebooting.", system.Address);
                tmpProcesses.UnionWith(system.Processes.Values.Where(p => p.ProcessContext.ParentPid == 0));
                foreach (var p in tmpProcesses)
                    CompleteRecurse(p, Process.CompletionKind.KillRemote);
                tmpProcesses.Clear();
                system.BootTime = Time + system.RebootDuration;
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
                        bool fail = operation.ProcessContext is ProgramContext pc0 && !pc0.User.Connected;
                        fail = fail || operation.ProcessContext.System.BootTime > Model.Now;
                        fail = fail || !Model.Systems.Contains(operation.ProcessContext.System);
                        fail = fail || !operation.ProcessContext.System.Logins.Contains(operation.ProcessContext.Login);
                        fail = fail ||
                               (operation is not ShellProcess ||
                                operation is ShellProcess { RemoteParent: null }) &&
                               operation.ProcessContext is ProgramContext pc2 &&
                               !pc2.Person.ShellChain.Contains(pc2.Shell);
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
                    _server.Logger.LogWarning(
                        "Unhandled exception occurred during a process update, killing process.\nException:\n{Exception}",
                        e);
                    processes.Remove(operation);
                    try
                    {
                        CompleteRecurse(operation, Process.CompletionKind.KillRemote);
                    }
                    catch (Exception e2)
                    {
                        _server.Logger.LogWarning(
                            "Unhandled exception occurred while killing process.\nException:\n{Exception}",
                            e2);
                    }
                }
            }
        }

        /// <inheritdoc />
        public T? GetPluginOfType<T>() where T : IWorldPlugin => _plugins.OfType<T>().FirstOrDefault();

        /// <inheritdoc />
        public bool TryGetSystem(Guid id, [NotNullWhen(true)] out SystemModel? system)
        {
            //system = Database.GetAsync<Guid, SystemModel>(id).Result;
            system = Model.Systems.FirstOrDefault(f => f.Key == id);
            return system != null;
        }

        /// <inheritdoc />
        public IEnumerable<SystemModel> SearchSystems(Guid? key, string? tag)
        {
            if (tag != null)
            {
                if (Model.TaggedSystems.TryGetValue(tag, out var list))
                    return key != null ? list.Where(s => s.SpawnGroup == key) : list;

                return Enumerable.Empty<SystemModel>();
            }

            if (key != null)
            {
                return Model.SpawnGroupSystems.TryGetValue(key.Value, out var list)
                    ? list
                    : Enumerable.Empty<SystemModel>();
            }

            return Model.Systems;
        }

        /// <inheritdoc />
        public IEnumerable<PersonModel> SearchPersons(Guid? key, string? tag)
        {
            if (tag != null)
            {
                if (Model.TaggedPersons.TryGetValue(tag, out var list))
                    return key != null ? list.Where(s => s.SpawnGroup == key) : list;

                return Enumerable.Empty<PersonModel>();
            }

            if (key != null)
            {
                return Model.SpawnGroupPersons.TryGetValue(key.Value, out var list)
                    ? list
                    : Enumerable.Empty<PersonModel>();
            }

            return Model.Persons;
        }

        /// <inheritdoc />
        public bool CompleteRecurse(Process process, Process.CompletionKind completionKind)
        {
            if (process.Completed != null) return true;
            var context = process.ProcessContext;
            var system = context.System;
            var processes = system.Processes;
            uint pid = context.Pid;
            processes.Remove(pid);
            if (process is ShellProcess shProc)
            {
                var chain = shProc.ProgramContext.Person.ShellChain;
                int shellIdx = chain.IndexOf(shProc);
                if (shellIdx != -1)
                {
                    int endIdx = chain.Count;
                    for (int i = endIdx - 1; i > shellIdx; i--)
                        if (!CompleteRecurse(chain[i], completionKind))
                            return false;
                    chain.RemoveAt(shellIdx);
                }

                // If this is a remote shell and it's being terminated, go back to host shell to terminate proxy
                if (shProc.RemoteParent != null &&
                    shProc.RemoteParent.Remotes.TryGetValue(system.Address, out var remote))
                {
                    CompleteRecurse(remote, Process.CompletionKind.KillRemote);
                    shProc.RemoteParent.Remotes.Remove(system.Address);
                }
            }

            if (!process.Complete(completionKind)) return false;

            if (context is ProgramContext pc)
            {
                string[]? chainLine = pc.ChainLine;
                if (chainLine != null && completionKind == Process.CompletionKind.Normal)
                {
                    var genPc = ServerUtil.InitTentativeProgramContext(pc.World, pc.OperationId, pc.User, pc.Person,
                        ServerUtil.UnsplitCommandLine(chainLine), conWidth: pc.ConWidth);
                    ExecuteCommand(genPc);
                }
            }

            var toKill = processes.Values.Where(p => p.ProcessContext.ParentPid == pid).ToList();
            foreach (var p in toKill)
                CompleteRecurse(p, completionKind);
            return true;
        }

        /// <inheritdoc />
        public ShellProcess? StartShell(IPersonContext personContext, PersonModel personModel,
            LoginModel loginModel, string[] argv, bool attach)
        {
            var programContext =
                ServerUtil.InitProgramContext(this, Guid.Empty, personContext, personModel, loginModel, argv);
            return StartShell(programContext, _server.IntrinsicPrograms[ServerConstants.ShellName].Item1(), attach);
        }

        /// <inheritdoc />
        public ProgramProcess? StartProgram(ShellProcess shell, string[] argv, string[]? hargv = null,
            Program? program = null)
        {
            if (argv.Length == 0 || string.IsNullOrWhiteSpace(argv[0])) return null;
            var shellContext = shell.ProgramContext;
            var user = shellContext.User;
            var system = shellContext.System;

            var programContext =
                ServerUtil.InitProgramContext(this, Guid.Empty, shellContext.User, shellContext.Person,
                    shellContext.Login, argv);
            programContext.ParentPid = shell.ProgramContext.Pid;
            programContext.Shell = shell;
            if (program != null)
            {
                programContext.HArgv = hargv ?? new[] { argv[0] };
                return StartProgram(programContext, program);
            }

            if (TryGetIntrinsicProgramWithHargs(programContext.Argv[0], out var intrinsicRes))
            {
                programContext.HArgv = intrinsicRes.Item3;
                return StartProgram(programContext, intrinsicRes.Item1);
            }

            string exe = $"/bin/{programContext.Argv[0]}";
            system.TryGetFile(exe, shellContext.Login, out var result, out var closestStr, out var fse,
                caseInsensitive: true);
            if (program == null)
                switch (result)
                {
                    case ReadAccessResult.NotReadable:
                        user.WriteEventSafe(Program.Output($"{closestStr}: Permission denied\n"));
                        user.FlushSafeAsync();
                        return null;
                    case ReadAccessResult.NoExist:
                        user.WriteEventSafe(Program.Output($"{closestStr}: No such file or directory\n"));
                        user.FlushSafeAsync();
                        return null;
                }

            if (fse != null && fse.Kind == FileModel.FileKind.ProgFile && TryGetProgramWithHargs(
                    system.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                    out var res))
            {
                programContext.HArgv = res.Item3;
                return StartProgram(programContext, res.Item1);
            }

            return null;
        }

        /// <inheritdoc />
        public ServiceProcess? StartService(LoginModel loginModel, string[] argv, string[]? hargv = null,
            Service? service = null)
        {
            if (argv.Length == 0 || string.IsNullOrWhiteSpace(argv[0])) return null;
            var system = loginModel.System;
            var serviceContext = new ServiceContext { World = this, Argv = argv, System = system, Login = loginModel };
            if (service != null)
            {
                serviceContext.HArgv = hargv ?? new[] { argv[0] };
                return StartService(serviceContext, service);
            }

            string exe = $"/bin/{serviceContext.Argv[0]}";
            if (system.TryGetFile(exe, loginModel, out _, out _, out var fse, caseInsensitive: true) &&
                fse.Kind == FileModel.FileKind.ProgFile && TryGetServiceWithHargs(
                    system.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                    out var res))
            {
                serviceContext.HArgv = res.Item3;
                return StartService(serviceContext, res.Item1);
            }

            return null;
        }

        private ShellProcess? StartShell(ProgramContext context, Program program, bool attach)
        {
            var system = context.System;
            var person = context.Person;
            var login = context.Login;
            program.ProcessContext = context;
            program.Context = context;
            if (program.GetStartupMemory() + context.System.GetUsedMemory() > context.System.SystemMemory)
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
            if (attach) chain.Add(process);
            Executable.TryWriteLog(this, time, system, login, ServerConstants.LogKind_Login, logBody, out _);
            return process;
        }

        private ProgramProcess? StartProgram(ProgramContext context, Program program)
        {
            program.ProcessContext = context;
            program.Context = context;
            if (program.GetStartupMemory() + context.System.GetUsedMemory() > context.System.SystemMemory)
                return null;
            var system = context.System;
            uint? pid = system.GetAvailablePid();
            if (pid == null) return null;
            context.Pid = pid.Value;

            var process = new ProgramProcess(program);
            system.Processes.Add(pid.Value, process);
            _processes.Add(process);

            return process;
        }

        private ServiceProcess? StartService(ServiceContext context, Service service)
        {
            service.ProcessContext = context;
            service.Context = context;
            if (service.GetStartupMemory() + context.System.GetUsedMemory() > context.System.SystemMemory)
                return null;
            var system = context.System;
            uint? pid = system.GetAvailablePid();
            if (pid == null) return null;
            context.Pid = pid.Value;

            var process = new ServiceProcess(service);
            system.Processes.Add(pid.Value, process);
            _processes.Add(process);

            return process;
        }

        /// <inheritdoc />
        public ProgramInfoAttribute? GetProgramInfo(string? argv)
        {
            if (argv == null) return null;
            string[] line = argv.SplitCommandLine();
            if (line.Length == 0 || string.IsNullOrWhiteSpace(line[0])) return null;
            if (_server.IntrinsicPrograms.TryGetValue(line[0], out var prog))
                return prog.Item2;
            if (_server.Programs.TryGetValue(line[0], out prog))
                return prog.Item2;
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<(Func<Program>, ProgramInfoAttribute)> IntrinsicPrograms => _server.IntrinsicPrograms.Values;

        /// <inheritdoc />
        public void ExecuteCommand(ProgramContext programContext)
        {
            var personModel = programContext.Person;

            if (personModel.ShellChain.Count == 0)
            {
                programContext.User.WriteEventSafe(new OperationCompleteEvent { Operation = programContext.OperationId });
                programContext.User.FlushSafeAsync();
                return;
            }

            var shell = personModel.ShellChain[^1];
            var systemModel = shell.ProcessContext.System;

            programContext.Argv = programContext.Type switch
            {
                ProgramContext.InvocationType.Connect => systemModel.ConnectCommandLine != null
                    ? systemModel.ConnectCommandLine.SplitCommandLine()
                    : Array.Empty<string>(),
                ProgramContext.InvocationType.StartUp => Model.StartupCommandLine != null
                    ? Model.StartupCommandLine.SplitCommandLine()
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
                    {
                        if (!programContext.IsAi)
                            programContext.User.WriteEventSafe(
                                Program.Output("Process creation failed: out of memory\n"));
                        programContext.User.WriteEventSafe(ServerUtil.CreatePromptEvent(programContext.Shell));
                        programContext.User.WriteEventSafe(
                            new OperationCompleteEvent { Operation = programContext.OperationId });
                        programContext.User.FlushSafeAsync();
                    }

                    return;
                }

                string exe = $"/bin/{programContext.Argv[0]}";
                systemModel.TryGetFile(exe, programContext.Login, out var result, out var closestStr, out var fse,
                    caseInsensitive: true,
                    hidden: programContext.Type == ProgramContext.InvocationType.StartUp ? null : false);
                switch (result)
                {
                    case ReadAccessResult.Readable:
                        if (fse != null && fse.Kind == FileModel.FileKind.ProgFile)
                        {
                            string content = fse.Content ?? "heathcliff";
                            if (TryGetProgramWithHargs(content, out var program))
                            {
                                programContext.HArgv = program.Item3;
                                bool success = StartProgram(programContext, program.Item1) != null;
                                if (!success)
                                {
                                    if (!programContext.IsAi)
                                        programContext.User.WriteEventSafe(
                                            Program.Output("Process creation failed: out of memory\n"));
                                    programContext.User.WriteEventSafe(
                                        ServerUtil.CreatePromptEvent(programContext.Shell));
                                    programContext.User.WriteEventSafe(
                                        new OperationCompleteEvent { Operation = programContext.OperationId });
                                    programContext.User.FlushSafeAsync();
                                }

                                return;
                            }

                            if (TryGetServiceWithHargs(content, out var service))
                            {
                                bool success = StartService(programContext.Login, programContext.Argv, service.Item3,
                                    service.Item1) != null;
                                if (!success)
                                {
                                    if (!programContext.IsAi)
                                        programContext.User.WriteEventSafe(
                                            Program.Output("Process creation failed: out of memory\n"));
                                }

                                programContext.User.WriteEventSafe(
                                    ServerUtil.CreatePromptEvent(programContext.Shell));
                                programContext.User.WriteEventSafe(
                                    new OperationCompleteEvent { Operation = programContext.OperationId });
                                programContext.User.FlushSafeAsync();

                                return;
                            }
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
                            programContext.User.WriteEventSafe(closestStr == "/bin"
                                ? Program.Output($"{exe}: not found\n")
                                : Program.Output($"{programContext.Argv[0]}: command not found\n"));
                        break;
                }
            }

            programContext.User.WriteEventSafe(ServerUtil.CreatePromptEvent(programContext.Shell));
            programContext.User.WriteEventSafe(new OperationCompleteEvent { Operation = programContext.OperationId });
            programContext.User.FlushSafeAsync();
        }

        private bool TryGetIntrinsicProgramWithHargs(string command,
            out (Program, ProgramInfoAttribute, string[]) result)
        {
            string[] line = command.SplitCommandLine();
            if (line.Length == 0 || string.IsNullOrWhiteSpace(line[0]))
            {
                result = default;
                return false;
            }

            bool success = _server.IntrinsicPrograms.TryGetValue(line[0], out var res);
            result = success ? (res.Item1(), res.Item2, line) : default;
            return success;
        }

        private bool TryGetProgramWithHargs(string command, out (Program, ProgramInfoAttribute?, string[]) result)
        {
            string[] line = command.SplitCommandLine();
            if (line.Length == 0 || string.IsNullOrWhiteSpace(line[0]))
            {
                result = default;
                return false;
            }

            string id = line[0];

            if (_server.Programs.TryGetValue(id, out var res))
            {
                result = (res.Item1(), res.Item2, line);
                return true;
            }

            foreach (var plugin in _plugins)
            {
                if (plugin.TryProvideProgram(command, line, out result)) return true;
            }

            result = default;
            return false;
        }

        private bool TryGetServiceWithHargs(string command, out (Service, ServiceInfoAttribute?, string[]) result)
        {
            string[] line = command.SplitCommandLine();
            if (line.Length == 0 || string.IsNullOrWhiteSpace(line[0]))
            {
                result = default;
                return false;
            }

            string id = line[0];

            if (_server.Services.TryGetValue(id, out var res))
            {
                result = (res.Item1(), res.Item2, line);
                return true;
            }

            foreach (var plugin in _plugins)
            {
                if (plugin.TryProvideService(command, line, out result)) return true;
            }

            result = default;
            return false;
        }
    }
}

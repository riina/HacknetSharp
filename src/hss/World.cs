﻿using System;
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
        public ISpawn Spawn { get; }
        public IServerDatabase Database { get; }
        public SystemTemplate PlayerSystemTemplate { get; }
        public double Time { get; set; }
        public double PreviousTime { get; set; }

        private readonly HashSet<Process> _processes;
        private readonly HashSet<Process> _removeProcesses;

        private readonly HashSet<ShellProcess> _shellProcesses;
        private readonly HashSet<ShellProcess> _removeShellProcesses;


        internal World(Server server, WorldModel model, IServerDatabase database, Spawn spawn)
        {
            Server = server;
            Model = model;
            Spawn = spawn;
            Database = database;
            PlayerSystemTemplate = server.Templates.SystemTemplates[model.PlayerSystemTemplate];
            _processes = new HashSet<Process>();
            _removeProcesses = new HashSet<Process>();
            _shellProcesses = new HashSet<ShellProcess>();
            _removeShellProcesses = new HashSet<ShellProcess>();
        }

        public void Tick()
        {
            TickSet(_processes, _removeProcesses);
            TickSet(_shellProcesses, _removeShellProcesses);
        }

        private void TickSet<T>(HashSet<T> processes, HashSet<T> removeProcesses) where T : Process
        {
            foreach (var operation in processes)
            {
                try
                {
                    if (!operation.Completed.HasValue)
                    {
                        bool fail = operation.Context is ProgramContext pc0 && !pc0.User.Connected;
                        fail = fail || operation.Context.System.BootTime > Model.Now;
                        fail = fail || !Model.Systems.Contains(operation.Context.System);
                        fail = fail || operation.Context is ProgramContext pc1 &&
                            !operation.Context.System.Logins.Contains(pc1.Login);
                        fail = fail || operation.Context is ProgramContext pc2 &&
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

                    removeProcesses.Add(operation);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    removeProcesses.Add(operation);
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

            processes.ExceptWith(removeProcesses);
        }

        public void CompleteRecurse(Process process, Process.CompletionKind completionKind)
        {
            var processes = process.Context.System.Processes;
            uint pid = process.Context.Pid;
            bool removed = processes.Remove(pid);
            if (process.Context is ProgramContext pc)
            {
                pc.Person.ShellChain.RemoveAll(p => p == process);
                uint shellPid = pc.Shell.ProgramContext.Pid;
                /*Console.WriteLine($"Testing {shellPid} {removed}...");
                foreach (var proc in processes)
                {
                    Console.WriteLine($"{proc.Value} {proc.Value.Context.ParentPid} {proc.Value.Context.Pid}");
                }*/

                if (removed && processes.Values.All(p => p.Context.Pid == shellPid || p.Context.ParentPid != shellPid))
                {
                    if (pc.Person.ShellChain.Count != 0)
                    {
                        pc.User.WriteEventSafe(CommonUtil.CreatePromptEvent(pc.Person.ShellChain[^1]));
                        pc.User.FlushSafeAsync();
                    }
                }
            }

            process.Complete(completionKind);
            process.Completed = completionKind;
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
                _processes.Add(new ProgramProcess(programContext, intrinsicRes.Item1));
                return;
            }

            if (!systemModel.DirectoryExists("/bin")) return;
            string exe = $"/bin/{programContext.Argv[0]}";
            if (!systemModel.FileExists(exe, true)) return;
            var fse = systemModel.GetFileSystemEntry(exe);
            if (fse != null && fse.Kind == FileModel.FileKind.ProgFile &&
                Server.Programs.TryGetValue(systemModel.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                    out var res)) _processes.Add(new ProgramProcess(programContext, res.Item1));
        }

        public void StartDaemon(SystemModel systemModel, string line)
        {
            var serviceContext = new ServiceContext
            {
                World = this, Argv = Arguments.SplitCommandLine(line), System = systemModel
            };
            if (!systemModel.DirectoryExists("/bin")) return;
            string exe = $"/bin/{serviceContext.Argv[0]}";
            if (!systemModel.FileExists(exe, true)) return;
            var fse = systemModel.GetFileSystemEntry(exe);
            if (fse != null && fse.Kind == FileModel.FileKind.ProgFile &&
                Server.Services.TryGetValue(systemModel.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                    out var res)) _processes.Add(new ServiceProcess(serviceContext, res));
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
                            Server.Programs.TryGetValue(systemModel.GetFileSystemEntry(exe)?.Content ?? "heathcliff",
                                out var res))
                        {
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
                programContext.User.WriteEventSafe(CommonUtil.CreatePromptEvent(programContext.Shell));
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
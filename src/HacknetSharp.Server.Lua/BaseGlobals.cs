using System;
using System.Collections.Generic;
using System.Linq;
using HacknetSharp.Server.Models;
using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;

namespace HacknetSharp.Server.Lua
{
    /// <summary>
    /// Standard globals.
    /// </summary>
    public class BaseGlobals
    {
        /// <summary>
        /// Globals provided by this instance.
        /// </summary>
        public Dictionary<string, object> MyGlobals { get; }

        private readonly ScriptManager _scriptManager;
        private readonly IWorld _world;

        /// <summary>
        /// Creates new instance of <see cref="BaseGlobals"/>.
        /// </summary>
        /// <param name="scriptManager">Base script manager.</param>
        /// <param name="world">World used for operation.</param>
        public BaseGlobals(ScriptManager scriptManager, IWorld world)
        {
            MyGlobals = new Dictionary<string, object>();
            _scriptManager = scriptManager;
            _world = world;

            MyGlobals["World"] = world;

            // Program and service members
            MyGlobals["Delay"] = scriptManager.EvaluateScript("return function(d) coroutine.yield(self.Delay(d)) end");

            // Program-only members
            MyGlobals["Confirm"] =
                scriptManager.EvaluateScript(
                    "return function() local c = self.Confirm(false) coroutine.yield(c) return c.Confirmed end");

            ScriptManager.LoadGlobalsFromObject(this, MyGlobals);
        }

        #region ScriptManager proxy functions

        #endregion

        #region Miscellaneous convenience functions

        #endregion

        #region Standard members

        #region Missions

        [Global(typeof(Func<PersonModel?, Guid?, MissionModel[]?>))]
        private MissionModel[]? Missions(PersonModel? person, Guid? campaignKey)
        {
            if (person == null) return null;
            return (campaignKey != null ? person.Missions.Where(m => m.CampaignKey == campaignKey) : person.Missions)
                .ToArray();
        }

        [Global(typeof(Func<PersonModel?, string, Guid?, MissionModel?>))]
        private MissionModel? StartMission(PersonModel? person, string missionPath, Guid? campaignKey)
        {
            campaignKey ??= Guid.NewGuid();
            if (person == null) return null;
            if (!_world.Templates.MissionTemplates.ContainsKey(missionPath)) return null;
            return _scriptManager.StartMission(person, missionPath, campaignKey.Value);
        }

        [Global(typeof(Func<PersonModel?, string, bool>))]
        private bool RemoveMission(PersonModel? person, string missionPath)
        {
            if (person == null) return false;
            if (!_world.Templates.MissionTemplates.ContainsKey(missionPath)) return false;
            var missions = person.Missions.Where(m => m.Template == missionPath).ToList();
            foreach (var mission in missions)
                _world.Spawn.RemoveMission(mission);
            return missions.Count != 0;
        }

        [Global(typeof(Action<Guid?>))]
        private void DropSpawns(Guid? key)
        {
            if (key == null) return;
            _world.Spawn.RemoveDependents(key.Value);
        }

        [Global(typeof(Action<PersonModel?, string>))]
        private void DropCampaign(PersonModel? person, string campaignName)
        {
            if (person == null) return;
            foreach (var mission in person.Missions.Where(m => m.Data.Campaign == campaignName).ToList())
            {
                _world.Spawn.RemoveMission(mission);
                _world.Spawn.RemoveDependents(mission.CampaignKey);
            }
        }

        [Global(typeof(Action<PersonModel?, Guid?>))]
        private void DropCampaignK(PersonModel? person, Guid? campaignKey)
        {
            if (person == null || campaignKey == null) return;
            foreach (var mission in person.Missions.Where(m => m.CampaignKey == campaignKey.Value).ToList())
                _world.Spawn.RemoveMission(mission);
            _world.Spawn.RemoveDependents(campaignKey.Value);
        }

        #endregion

        #region Persons

        [Global(typeof(Func<string, PersonModel[]?>))]
        private PersonModel[] PersonT(string tag)
        {
            return _world.Model.TaggedPersons.TryGetValue(tag, out var person)
                ? person.ToArray()
                : Array.Empty<PersonModel>();
        }

        [Global(typeof(Func<Guid?, string?, PersonModel[]>))]
        private PersonModel[] PersonGT(Guid? key, string? tag)
        {
            return _world.SearchPersons(key, tag).ToArray();
        }

        [Global(typeof(Func<Guid?, string?, PersonModel?>))]
        private PersonModel? PersonGTSingle(Guid? key, string? tag)
        {
            return _world.SearchPersons(key, tag).FirstOrDefault();
        }

        [Global(typeof(Func<string, string, PersonModel>))]
        private PersonModel SpawnPerson(string name, string username)
        {
            return _world.Spawn.Person(name, username);
        }

        [Global(typeof(Func<string, string, Guid, PersonModel>))]
        private PersonModel SpawnPersonG(string name, string username, Guid key)
        {
            return _world.Spawn.Person(name, username, null, key);
        }

        [Global(typeof(Func<string, string, string, PersonModel>))]
        private PersonModel SpawnPersonT(string name, string username, string tag)
        {
            return _world.Spawn.Person(name, username, tag);
        }

        [Global(typeof(Func<string, string, Guid, string, PersonModel>))]
        private PersonModel SpawnPersonGT(string name, string username, Guid key, string tag)
        {
            return _world.Spawn.Person(name, username, tag, key);
        }

        [Global(typeof(Func<string, string, Guid, string, PersonModel>))]
        private PersonModel EnsurePersonGT(string name, string username, Guid key, string tag)
        {
            return _world.SearchPersons(key, tag).FirstOrDefault()
                   ?? _world.Spawn.Person(name, username, tag, key);
        }

        [Global(typeof(Action<PersonModel?>))]
        private void RemovePerson(PersonModel? person)
        {
            if (person == null) return;
            _world.Spawn.RemovePerson(person);
        }

        #endregion

        #region Systems

        [Global(typeof(Func<string, SystemModel[]?>))]
        private SystemModel[] SystemT(string tag)
        {
            return _world.Model.TaggedSystems.TryGetValue(tag, out var system)
                ? system.ToArray()
                : Array.Empty<SystemModel>();
        }

        [Global(typeof(Func<Guid?, string?, SystemModel[]>))]
        private SystemModel[] SystemGT(Guid? key, string? tag)
        {
            return _world.SearchSystems(key, tag).ToArray();
        }

        [Global(typeof(Func<Guid?, string?, SystemModel?>))]
        private SystemModel? SystemGTSingle(Guid? key, string? tag)
        {
            return _world.SearchSystems(key, tag).FirstOrDefault();
        }

        [Global(typeof(Func<string, SystemModel?>))]
        private SystemModel? SystemA(string address)
        {
            if (!IPAddressRange.TryParse(address, false, out var addr) ||
                !addr.TryGetIPv4HostAndSubnetMask(out uint host, out _))
                return null;
            return _world.Model.AddressedSystems.TryGetValue(host, out var system) ? system : null;
        }

        [Global(typeof(Func<PersonModel?, SystemModel?>))]
        private SystemModel? Home(PersonModel? person)
        {
            if (person == null) return null;
            var key = person.DefaultSystem;
            return person.Systems.FirstOrDefault(s => s.Key == key);
        }

        [Global(typeof(Func<SystemModel?, bool>))]
        private bool SystemUp(SystemModel? system)
        {
            if (system == null) return false;
            return system.BootTime <= _world.Time;
        }

        [Global(typeof(Func<PersonModel?, string, string, string, SystemModel?>))]
        private SystemModel? SpawnSystem(PersonModel? owner, string password, string template, string addressRange)
        {
            return SpawnSystemBase(owner, password, template, addressRange);
        }

        [Global(typeof(Func<PersonModel?, string, string, string, Guid, SystemModel?>))]
        private SystemModel? SpawnSystemG(PersonModel? owner, string password, string template, string addressRange,
            Guid key)
        {
            return SpawnSystemBase(owner, password, template, addressRange, null, key);
        }

        [Global(typeof(Func<PersonModel?, string, string, string, string, SystemModel?>))]
        private SystemModel? SpawnSystemT(PersonModel? owner, string password, string template, string addressRange,
            string tag)
        {
            return SpawnSystemBase(owner, password, template, addressRange, tag);
        }

        [Global(typeof(Func<PersonModel?, string, string, string, Guid, string, SystemModel?>))]
        private SystemModel? SpawnSystemGT(PersonModel? owner, string password, string template, string addressRange,
            Guid key, string tag)
        {
            return SpawnSystemBase(owner, password, template, addressRange, tag, key);
        }

        [Global(typeof(Func<PersonModel?, string, string, string, Guid, string, SystemModel?>))]
        private SystemModel? EnsureSystemGT(PersonModel? owner, string password, string template, string addressRange,
            Guid key, string tag)
        {
            return _world.SearchSystems(key, tag).FirstOrDefault()
                   ?? SpawnSystemBase(owner, password, template, addressRange, tag, key);
        }

        private SystemModel? SpawnSystemBase(PersonModel? owner, string pass, string template, string addressRange,
            string? tag = null, Guid? key = null)
        {
            if (owner == null) return null;
            if (!_world.Templates.SystemTemplates.TryGetValue(template, out var sysTemplate)) return null;
            if (!IPAddressRange.TryParse(addressRange, true, out var range)) return null;
            var password = ServerUtil.HashPassword(pass);
            return _world.Spawn.System(sysTemplate, template, owner, password, range, tag, key);
        }

        [Global(typeof(Action<SystemModel?>))]
        private void RemoveSystem(SystemModel? system)
        {
            if (system == null) return;
            _world.Spawn.RemoveSystem(system);
        }

        [Global(typeof(Action<SystemModel?>))]
        private void ResetSystem(SystemModel? system)
        {
            if (system == null) return;
            if (!_world.Templates.SystemTemplates.TryGetValue(system.Template, out var template)) return;
            template.ApplyTemplate(_world.Spawn, system);
        }

        #endregion

        #region Files

        [Global(typeof(Func<SystemModel?, string, bool>))]
        private bool FileExists(SystemModel? system, string path)
        {
            if (system == null) return false;
            path = Executable.GetNormalized(path);
            return system.Files.Any(f => f.FullPath == path);
        }

        [Global(typeof(Func<SystemModel?, string, string, bool, bool>))]
        private bool FileContains(SystemModel? system, string path, string substring, bool ignoreCase)
        {
            path = Executable.GetNormalized(path);
            var file = system?.Files.FirstOrDefault(f => f.FullPath == path);
            return file?.Content != null && file.Content.Contains(substring,
                ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
        }

        [Global(typeof(Func<SystemModel?, string, FileModel?>))]
        internal FileModel? File(SystemModel? system, string path)
        {
            return system?.GetFileSystemEntry(path);
        }

        [Global(typeof(Func<SystemModel?, string, FileModel[]?>))]
        internal FileModel[]? Folder(SystemModel? system, string path)
        {
            if (system == null) return null;
            path = Executable.GetNormalized(path);
            if (path != "/")
            {
                var file = system.GetFileSystemEntry(path);
                if (file == null || file.Kind != FileModel.FileKind.Folder) return null;
            }

            return system.EnumerateDirectory(path).ToArray();
        }

        [Global(typeof(Func<SystemModel?, string, string, FileModel?>))]
        private FileModel? SpawnFile(SystemModel? system, string path, string content)
        {
            if (system == null) return null;
            var owner = system.Owner.Key;
            var login = system.Logins.FirstOrDefault(l => l.Person == owner);
            if (login == null) return null;
            path = Executable.GetNormalized(path);
            var existing = system.Files.FirstOrDefault(f => f.FullPath == path);
            if (existing != null) return existing;
            return _world.Spawn.TextFile(system, login, path, content);
        }

        [Global(typeof(Func<SystemModel?, string, string, FileModel?>))]
        private FileModel? SpawnFolder(SystemModel? system, string path, string content)
        {
            if (system == null) return null;
            var owner = system.Owner.Key;
            var login = system.Logins.FirstOrDefault(l => l.Person == owner);
            if (login == null) return null;
            path = Executable.GetNormalized(path);
            var existing = system.Files.FirstOrDefault(f => f.FullPath == path);
            if (existing != null) return existing;
            return _world.Spawn.Folder(system, login, path, content);
        }

        [Global(typeof(Action<FileModel?>))]
        private void RemoveFile(FileModel? file)
        {
            if (file == null) return;
            _world.Spawn.RemoveFile(file);
        }

        #endregion

        #region Tasks

        [Global(typeof(Func<SystemModel?, string, float, float, float, CronModel?>))]
        private CronModel? SpawnCron(SystemModel? system, string script, float start, float delay, float end)
        {
            if (system == null) return null;
            var cron = _world.Spawn.Cron(system, script, start, delay, end);
            _scriptManager.AssignCronTask(cron, _scriptManager.EvaluateScript(script));
            return cron;
        }

        [Global(typeof(Action<CronModel?>))]
        private void RemoveCron(CronModel? cron)
        {
            if (cron == null) return;
            _world.Spawn.RemoveCron(cron);
        }

        #endregion

        #region General

        [Global(typeof(Action<string>))]
        private void Log(string text)
        {
            _world.Logger.LogInformation(text);
        }

        [Global(typeof(Action<string, int>))]
        private void LogEx(string text, int level)
        {
            var lv = level switch
            {
                0 => LogLevel.Information,
                1 => LogLevel.Warning,
                2 => LogLevel.Error,
                _ => LogLevel.Critical
            };
            _world.Logger.Log(lv, text);
        }

        [Global(typeof(Func<PersonModel?, SystemModel?, (ShellProcess, LoginModel)?>))]
        private (ShellProcess, LoginModel)? StartShell(PersonModel? person, SystemModel? system)
        {
            if (person == null || system == null) return null;
            var login = system.Logins.FirstOrDefault(l => l.Person == person.Key);
            if (login == null) return null;
            var shell = _world.StartShell(new AIPersonContext(person), person, login,
                new[] { ServerConstants.ShellName, "HIVE" },
                false);
            if (shell == null) return null;
            return (shell, login);
        }

        [Global(typeof(Action<Process?>))]
        private void KillProcess(Process? process)
        {
            if (process == null) return;
            _world.CompleteRecurse(process, Process.CompletionKind.KillLocal);
        }

        [Global(typeof(Func<SystemModel?, Process[]?>))]
        private Process[]? Ps(SystemModel? system)
        {
            if (system == null) return null;
            return system.Ps(null, null, null).ToArray();
        }

        [Global(typeof(Func<LoginModel?, Process[]?>))]
        private Process[]? PsLogin(LoginModel? login)
        {
            if (login == null) return null;
            return login.System.Ps(login, null, null).ToArray();
        }

        [Global(typeof(Action<Guid, string, string, string>))]
        private void RunRandoHackScript(Guid key, string systemTag, string personTag, string script)
        {
            var system = SystemGTSingle(key, systemTag);
            if (system == null) return;
            var person = PersonGTSingle(key, personTag);
            if (person == null) return;
            RunHackScriptBase(system, person, script, key);
        }

        [Global(typeof(Action<Guid, string, string>))]
        private void RunHackScript(Guid key, string systemTag, string script)
        {
            var system = SystemGTSingle(key, systemTag);
            if (system == null) return;
            RunHackScriptBase(system, system.Owner, script, key);
        }

        private void RunHackScriptBase(SystemModel system, PersonModel person, string script, Guid key)
        {
            LuaProgram program;
            try
            {
                if (!_scriptManager.TryGetScriptFile(script, out var scriptDyn)) return;
                program = new LuaProgram(_scriptManager.GetCoroutine(scriptDyn),
                    new Dictionary<string, object> { { "key", key } });
            }
            catch
            {
                return;
            }

            var shellRes = StartShell(person, system);
            if (shellRes == null) return;
            var (shell, login) = shellRes.Value;
            var process = _world.StartProgram(shell, new[] { script }, null, program);
            if (process == null)
            {
                _world.CompleteRecurse(shell, Process.CompletionKind.Normal);
                return;
            }

            var service = new HackScriptHostService(shell, process);
            var serviceProcess = _world.StartService(login, new[] { "HACKSCRIPT_HOST" }, null, service);
            if (serviceProcess == null)
            {
                _world.CompleteRecurse(shell, Process.CompletionKind.Normal);
            }
        }

        [IgnoreRegistration]
        private class HackScriptHostService : Service
        {
            private readonly ShellProcess _shell;
            private readonly Process _process;

            public HackScriptHostService(ShellProcess shell, Process process)
            {
                _shell = shell;
                _process = process;
            }

            public override IEnumerator<YieldToken?> Run()
            {
                // The shell should be a child of this host service, so change its parent PID here
                _shell.ProgramContext.ParentPid = Pid;
                // Hold until child program is done executing, then exit after
                while (_process.Completed == null)
                    yield return null;
                // Gracefully kill shell process
                World.CompleteRecurse(_shell, Process.CompletionKind.Normal);
            }
        }

        #endregion

        #endregion

        #region Program and service

        #endregion

        #region Program-only members

        [Global(typeof(Action<object>))]
        private void Write(object obj)
        {
            _scriptManager.GetGlobalAs<Program>("self")?.Write(obj);
        }

        [Global(typeof(Action))]
        private void Flush()
        {
            _scriptManager.GetGlobalAs<Program>("self")?.Flush();
        }

        [Global(typeof(Action))]
        private void Unbind()
        {
            _scriptManager.GetGlobalAs<Program>("self")?.SignalUnbindProcess();
        }

        [Global(typeof(Action<ShellProcess?, string>))]
        private void QueueInput(ShellProcess? shell, string input)
        {
            if (shell?.ProgramContext.User is not AIPersonContext apc) return;
            apc.InputQueue.Enqueue(input);
        }

        [Global(typeof(Action<ShellProcess?, DynValue>))]
        private void QueueEdit(ShellProcess? shell, DynValue function)
        {
            if (function.Type != DataType.Function) return;
            if (shell?.ProgramContext.User is not AIPersonContext apc) return;
            apc.EditQueue.Enqueue(s =>
            {
                try
                {
                    return _scriptManager.RunScript<DynValue>(function, s)?.CastToString() ?? "";
                }
                catch
                {
                    return "";
                }
            });
        }

        [Global(typeof(Action<ShellProcess?, string>))]
        private void QueueFixedEdit(ShellProcess? shell, string content)
        {
            if (shell?.ProgramContext.User is not AIPersonContext apc) return;
            apc.EditQueue.Enqueue(_ => content);
        }

        #endregion
    }
}

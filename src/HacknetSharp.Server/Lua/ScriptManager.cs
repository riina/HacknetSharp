using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using HacknetSharp.Server.Models;
using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;

namespace HacknetSharp.Server.Lua
{
    /// <summary>
    /// Script context manager.
    /// </summary>
    /// <remarks>
    /// Based on<br/>
    /// https://github.com/teamRokuro/NetBattle/blob/e273fa58117bcdb6383255ecdebd4a95f1c46d93/NetBattle/Field/FieldManager.cs
    /// </remarks>
    public class ScriptManager
    {
        static ScriptManager() => ScriptUtil.Init();

        private readonly IWorld _world;
        private readonly Script _script;
        private readonly Dictionary<string, DynValue> _expressions;

        /// <summary>
        /// Creates a new instance of <see cref="ScriptManager"/>.
        /// </summary>
        /// <param name="world">World context.</param>
        public ScriptManager(IWorld world)
        {
            _world = world;
            _script = new Script(CoreModules.Preset_SoftSandbox);
            _script.Globals["world"] = _world;
            _expressions = new Dictionary<string, DynValue>();

            #region Function registration

            // Manager

            // Misc convenience

            // Standard members

            // Missions
            RegisterFunction<PersonModel?, string, MissionModel?>(nameof(StartMission), StartMission);
            RegisterFunction<PersonModel?, string, bool>(nameof(RemoveMission), RemoveMission);

            // Persons
            RegisterFunction<string, PersonModel[]?>(nameof(PersonT), PersonT);
            RegisterFunction<Guid?, string?, PersonModel[]>(nameof(PersonGT), PersonGT);
            RegisterFunction<Guid?, string?, PersonModel?>(nameof(PersonGTSingle), PersonGTSingle);
            RegisterFunction<string, string, PersonModel>(nameof(SpawnPerson), SpawnPerson);
            RegisterFunction<string, string, Guid, PersonModel>(nameof(SpawnPersonG), SpawnPersonG);
            RegisterFunction<string, string, string, PersonModel>(nameof(SpawnPersonT), SpawnPersonT);
            RegisterFunction<string, string, Guid, string, PersonModel>(nameof(SpawnPersonGT), SpawnPersonGT);
            RegisterFunction<string, string, Guid, string, PersonModel>(nameof(EnsurePersonGT), EnsurePersonGT);
            RegisterAction<PersonModel?>(nameof(RemovePerson), RemovePerson);

            // Systems
            RegisterFunction<string, SystemModel[]?>(nameof(SystemT), SystemT);
            RegisterFunction<Guid?, string?, SystemModel[]>(nameof(SystemGT), SystemGT);
            RegisterFunction<Guid?, string?, SystemModel?>(nameof(SystemGTSingle), SystemGTSingle);
            RegisterFunction<string, SystemModel?>(nameof(SystemA), SystemA);
            RegisterFunction<PersonModel?, SystemModel?>(nameof(Home), Home);
            RegisterFunction<SystemModel?, bool>(nameof(SystemUp), SystemUp);
            RegisterFunction<PersonModel?, string, string, string, SystemModel?>(nameof(SpawnSystem), SpawnSystem);
            RegisterFunction<PersonModel?, string, string, string, Guid, SystemModel?>(nameof(SpawnSystemG),
                SpawnSystemG);
            RegisterFunction<PersonModel?, string, string, string, string, SystemModel?>(nameof(SpawnSystemT),
                SpawnSystemT);
            RegisterFunction<PersonModel?, string, string, string, Guid, string, SystemModel?>(nameof(SpawnSystemGT),
                SpawnSystemGT);
            RegisterFunction<PersonModel?, string, string, string, Guid, string, SystemModel?>(nameof(EnsureSystemGT),
                EnsureSystemGT);
            RegisterAction<SystemModel?>(nameof(RemoveSystem), RemoveSystem);
            RegisterAction<SystemModel?>(nameof(ResetSystem), ResetSystem);

            // Files
            RegisterFunction<SystemModel?, string, bool>(nameof(FileExists), FileExists);
            RegisterFunction<SystemModel?, string, string, bool, bool>(nameof(FileContains), FileContains);
            RegisterFunction<SystemModel?, string, FileModel?>(nameof(File), File);
            RegisterFunction<SystemModel?, string, DynValue?>(nameof(Folder), Folder);
            RegisterFunction<SystemModel?, string, string, FileModel?>(nameof(SpawnFile), SpawnFile);
            RegisterFunction<SystemModel?, string, string, FileModel?>(nameof(SpawnFolder), SpawnFolder);
            RegisterAction<FileModel?>(nameof(RemoveFile), RemoveFile);

            // Tasks
            RegisterFunction<SystemModel?, string, float, float, float, CronModel?>(nameof(SpawnCron), SpawnCron);
            RegisterAction<CronModel?>(nameof(RemoveCron), RemoveCron);

            // Generals
            RegisterAction<string>(nameof(Log), Log);
            RegisterAction<string, int>(nameof(LogEx), LogEx);
            RegisterFunction<PersonModel?, SystemModel?, (ShellProcess, LoginModel)?>(nameof(StartShell), StartShell);
            RegisterAction<Process?>(nameof(KillProcess), KillProcess);
            RegisterFunction<SystemModel?, DynValue?>(nameof(Ps), Ps);
            RegisterFunction<LoginModel?, DynValue?>(nameof(PsLogin), PsLogin);
            RegisterAction<Guid, string, string, string>(nameof(RunRandoHackScript), RunRandoHackScript);
            RegisterAction<Guid, string, string>(nameof(RunHackScript), RunHackScript);

            // Program and service members
            RunVoidScript(@"function Delay(d) coroutine.yield(self.Delay(d)) end");

            // Program-only members
            RunVoidScript(@"function Write(obj) return self.Write(obj) end");
            RunVoidScript(@"function Flush() return self.Flush() end");
            RunVoidScript(@"function Unbind() return self.SignalUnbindProcess() end");
            RunVoidScript(
                @"function Confirm() local c = self.Confirm(false) coroutine.yield(c) return c.Confirmed end");
            RegisterAction<ShellProcess?, string>(nameof(QueueInput), QueueInput);
            RegisterAction<ShellProcess?, DynValue>(nameof(QueueEdit), QueueEdit);
            RegisterAction<ShellProcess?, string>(nameof(QueueFixedEdit), QueueFixedEdit);

            #endregion
        }

        #region ScriptManager proxy functions

        #endregion

        #region Miscellaneous convenience functions

        /*private static Cell2 CvCell2(int x, int y) => new Cell2(x, y);*/

        #endregion

        #region Standard members

        #region Missions

        private MissionModel? StartMission(PersonModel? person, string missionPath)
        {
            if (person == null) return null;
            if (!_world.Templates.MissionTemplates.ContainsKey(missionPath)) return null;
            return _world.StartMission(person, missionPath);
        }

        private bool RemoveMission(PersonModel? person, string missionPath)
        {
            if (person == null) return false;
            if (!_world.Templates.MissionTemplates.ContainsKey(missionPath)) return false;
            var mission = person.Missions.FirstOrDefault(m => m.Template == missionPath);
            if (mission == null) return false;
            _world.Spawn.RemoveMission(mission);
            return true;
        }

        #endregion

        #region Persons

        private PersonModel[] PersonT(string tag)
        {
            return _world.Model.TaggedPersons.TryGetValue(tag, out var person)
                ? person.ToArray()
                : Array.Empty<PersonModel>();
        }

        private PersonModel[] PersonGT(Guid? key, string? tag)
        {
            return _world.SearchPersons(key, tag).ToArray();
        }

        private PersonModel? PersonGTSingle(Guid? key, string? tag)
        {
            return _world.SearchPersons(key, tag).FirstOrDefault();
        }

        private PersonModel SpawnPerson(string name, string username)
        {
            return _world.Spawn.Person(name, username);
        }

        private PersonModel SpawnPersonG(string name, string username, Guid key)
        {
            return _world.Spawn.Person(name, username, null, key);
        }

        private PersonModel SpawnPersonT(string name, string username, string tag)
        {
            return _world.Spawn.Person(name, username, tag);
        }

        private PersonModel SpawnPersonGT(string name, string username, Guid key, string tag)
        {
            return _world.Spawn.Person(name, username, tag, key);
        }

        private PersonModel EnsurePersonGT(string name, string username, Guid key, string tag)
        {
            return _world.SearchPersons(key, tag).FirstOrDefault()
                   ?? _world.Spawn.Person(name, username, tag, key);
        }

        private void RemovePerson(PersonModel? person)
        {
            if (person == null) return;
            _world.Spawn.RemovePerson(person);
        }

        #endregion

        #region Systems

        private SystemModel[] SystemT(string tag)
        {
            return _world.Model.TaggedSystems.TryGetValue(tag, out var system)
                ? system.ToArray()
                : Array.Empty<SystemModel>();
        }

        private SystemModel[] SystemGT(Guid? key, string? tag)
        {
            return _world.SearchSystems(key, tag).ToArray();
        }

        private SystemModel? SystemGTSingle(Guid? key, string? tag)
        {
            return _world.SearchSystems(key, tag).FirstOrDefault();
        }

        private SystemModel? SystemA(string address)
        {
            if (!IPAddressRange.TryParse(address, false, out var addr) ||
                !addr.TryGetIPv4HostAndSubnetMask(out uint host, out _))
                return null;
            return _world.Model.AddressedSystems.TryGetValue(host, out var system) ? system : null;
        }


        private SystemModel? Home(PersonModel? person)
        {
            if (person == null) return null;
            var key = person.DefaultSystem;
            return person.Systems.FirstOrDefault(s => s.Key == key);
        }

        private bool SystemUp(SystemModel? system)
        {
            if (system == null) return false;
            return system.BootTime <= _world.Time;
        }

        private SystemModel? SpawnSystem(PersonModel? owner, string password, string template, string addressRange)
        {
            return SpawnSystemBase(owner, password, template, addressRange);
        }


        private SystemModel? SpawnSystemG(PersonModel? owner, string password, string template, string addressRange,
            Guid key)
        {
            return SpawnSystemBase(owner, password, template, addressRange, null, key);
        }

        private SystemModel? SpawnSystemT(PersonModel? owner, string password, string template, string addressRange,
            string tag)
        {
            return SpawnSystemBase(owner, password, template, addressRange, tag);
        }

        private SystemModel? SpawnSystemGT(PersonModel? owner, string password, string template, string addressRange,
            Guid key, string tag)
        {
            return SpawnSystemBase(owner, password, template, addressRange, tag, key);
        }

        private SystemModel? EnsureSystemGT(PersonModel? owner, string password, string template, string addressRange,
            Guid key, string tag)
        {
            return _world.SearchSystems(key, tag).FirstOrDefault()
                   ?? SpawnSystemBase(owner, password, template, addressRange, tag, key);
        }

        private SystemModel? SpawnSystemBase(PersonModel? owner, string password, string template, string addressRange,
            string? tag = null, Guid? key = null)
        {
            if (owner == null) return null;
            if (!_world.Templates.SystemTemplates.TryGetValue(template, out var sysTemplate)) return null;
            if (!IPAddressRange.TryParse(addressRange, true, out var range)) return null;
            var (hash, salt) = ServerUtil.HashPassword(password);
            return _world.Spawn.System(sysTemplate, template, owner, hash, salt, range, tag, key);
        }

        private void RemoveSystem(SystemModel? system)
        {
            if (system == null) return;
            _world.Spawn.RemoveSystem(system);
        }

        private void ResetSystem(SystemModel? system)
        {
            if (system == null) return;
            if (!_world.Templates.SystemTemplates.TryGetValue(system.Template, out var template)) return;
            template.ApplyTemplate(_world.Spawn, system);
        }

        #endregion

        #region Files

        private bool FileExists(SystemModel? system, string path)
        {
            if (system == null) return false;
            path = Executable.GetNormalized(path);
            return system.Files.Any(f => f.FullPath == path);
        }

        private bool FileContains(SystemModel? system, string path, string substring, bool ignoreCase)
        {
            path = Executable.GetNormalized(path);
            var file = system?.Files.FirstOrDefault(f => f.FullPath == path);
            return file?.Content != null && file.Content.Contains(substring,
                ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture);
        }

        internal FileModel? File(SystemModel? system, string path)
        {
            return system?.GetFileSystemEntry(path);
        }

        internal DynValue? Folder(SystemModel? system, string path)
        {
            if (system == null) return null;
            path = Executable.GetNormalized(path);
            if (path != "/")
            {
                var file = system.GetFileSystemEntry(path);
                if (file == null || file.Kind != FileModel.FileKind.Folder) return null;
            }

            return PopulateNewtable(system.EnumerateDirectory(path));
        }

        private DynValue PopulateNewtable<TElement>(IEnumerable<TElement> enumerable)
        {
            var table = DynValue.NewTable(_script);
            var tb = table.Table;
            using var iterator = enumerable.GetEnumerator();
            for (int i = 0; iterator.MoveNext(); i++)
                tb[i + 1] = iterator.Current;
            return table;
        }

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

        private void RemoveFile(FileModel? file)
        {
            if (file == null) return;
            _world.Spawn.RemoveFile(file);
        }

        #endregion

        #region Tasks

        private CronModel? SpawnCron(SystemModel? system, string script, float start, float delay, float end)
        {
            if (system == null) return null;
            var cron = _world.Spawn.Cron(system, script, start, delay, end);
            cron.Task = EvaluateScript(script);
            return cron;
        }

        private void RemoveCron(CronModel? cron)
        {
            if (cron == null) return;
            _world.Spawn.RemoveCron(cron);
        }

        #endregion

        #region General

        private void Log(string text)
        {
            _world.Logger.LogInformation(text);
        }

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

        private (ShellProcess, LoginModel)? StartShell(PersonModel? person, SystemModel? system)
        {
            if (person == null || system == null) return null;
            var login = system.Logins.FirstOrDefault(l => l.Person == person.Key);
            if (login == null) return null;
            var shell = _world.StartShell(new AIPersonContext(person), person, login,
                new[] {ServerConstants.ShellName, "HIVE"},
                false);
            if (shell == null) return null;
            return (shell, login);
        }

        private void KillProcess(Process? process)
        {
            if (process == null) return;
            _world.CompleteRecurse(process, Process.CompletionKind.KillLocal);
        }

        private DynValue? Ps(SystemModel? system)
        {
            if (system == null) return null;
            return PopulateNewtable(system.Ps(null, null, null));
        }

        private DynValue? PsLogin(LoginModel? login)
        {
            if (login == null) return null;
            return PopulateNewtable(login.System.Ps(login, null, null));
        }

        private void RunRandoHackScript(Guid hostKey, string systemTag, string personTag, string script)
        {
            var system = SystemGTSingle(hostKey, systemTag);
            if (system == null) return;
            var person = PersonGTSingle(hostKey, personTag);
            if (person == null) return;
            RunHackScriptBase(system, person, script);
        }

        private void RunHackScript(Guid hostKey, string systemTag, string script)
        {
            var system = SystemGTSingle(hostKey, systemTag);
            if (system == null) return;
            RunHackScriptBase(system, system.Owner, script);
        }

        private void RunHackScriptBase(SystemModel system, PersonModel person, string script)
        {
            LuaProgram program;
            try
            {
                if (!_world.TryGetScriptFile(script, out var scriptDyn)) return;
                program = new LuaProgram(GetCoroutine(scriptDyn));
            }
            catch
            {
                return;
            }

            var shellRes = StartShell(person, system);
            if (shellRes == null) return;
            var (shell, login) = shellRes.Value;
            var process = _world.StartProgram(shell, new[] {script}, null, program);
            if (process == null)
            {
                _world.CompleteRecurse(shell, Process.CompletionKind.Normal);
                return;
            }

            var service = new HackScriptHostService(shell, process);
            var serviceProcess = _world.StartService(login, new[] {"HACKSCRIPT_HOST"}, null, service);
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

        /// <summary>
        /// Sets value in global table.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void SetGlobal(string key, object? value)
            => _script.Globals[key] = value;

        /// <summary>
        /// Clears value in global table.
        /// </summary>
        /// <param name="key">Key.</param>
        public void ClearGlobal(string key)
            => _script.Globals[key] = DynValue.Nil;

        #endregion

        #endregion

        #region Program and service

        #endregion

        #region Program-only members

        private void QueueInput(ShellProcess? shell, string input)
        {
            if (shell?.ProgramContext.User is not AIPersonContext apc) return;
            apc.InputQueue.Enqueue(input);
        }

        private void QueueEdit(ShellProcess? shell, DynValue function)
        {
            if (function.Type != DataType.Function) return;
            if (shell?.ProgramContext.User is not AIPersonContext apc) return;
            apc.EditQueue.Enqueue(s =>
            {
                try
                {
                    return _script.Call(function, s).CastToString();
                }
                catch
                {
                    return "";
                }
            });
        }

        private void QueueFixedEdit(ShellProcess? shell, string content)
        {
            if (shell?.ProgramContext.User is not AIPersonContext apc) return;
            apc.EditQueue.Enqueue(_ => content);
        }

        #endregion

        #region Script functions

        /// <summary>
        /// Executes and returns the raw result of a raw lua script.
        /// </summary>
        /// <param name="script">Script.</param>
        /// <param name="errorToString">If true, returns error as string.</param>
        /// <returns>Result.</returns>
        public DynValue EvaluateScript(string script, bool errorToString = false)
        {
            try
            {
                return _script.DoString(script);
            }
            catch (Exception e)
            {
                if (!errorToString) throw;
                return DynValue.FromObject(_script, e.ToString());
            }
        }

        /// <summary>
        /// Creates a dynamic expression.
        /// </summary>
        /// <param name="expression">Expression.</param>
        /// <returns>Expression.</returns>
        public DynamicExpression CreateDynamicExpression(string expression)
        {
            return _script.CreateDynamicExpression(expression);
        }

        /// <summary>
        /// Evaluates an expression.
        /// </summary>
        /// <param name="expression">Expression.</param>
        /// <param name="errorToString">If true, returns error as string.</param>
        /// <returns>Result.</returns>
        public DynValue EvaluateExpression(string expression, bool errorToString = false)
        {
            try
            {
                return CreateDynamicExpression(expression).Evaluate();
            }
            catch (Exception e)
            {
                if (!errorToString) throw;
                return DynValue.FromObject(_script, e.ToString());
            }
        }

        /// <summary>
        /// Gets string from a dynvalue.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <returns>String</returns>
        public string GetString(DynValue value)
        {
            var obj = value.ToObject();
            return obj is string str ? str : obj.ToString() ?? "<object>";
        }

        /// <summary>
        /// Registers a raw lua expression.
        /// </summary>
        /// <param name="key">Script key.</param>
        /// <param name="expression">Expression.</param>
        /// <returns>New value or existing value.</returns>
        public DynValue RegisterExpression(string key, string expression)
        {
            if (!_expressions.TryGetValue(key, out var dyn))
                _expressions[key] = dyn = _script.DoString(expression);
            return dyn;
        }

        /// <summary>
        /// Registers a raw lua expression.
        /// </summary>
        /// <param name="key">Script key.</param>
        /// <param name="stream">Expression stream.</param>
        /// <returns>New value or existing value.</returns>
        public DynValue RegisterExpression(string key, Stream stream)
        {
            if (!_expressions.TryGetValue(key, out var dyn))
                _expressions[key] = dyn = _script.DoStream(stream);
            return dyn;
        }

        /// <summary>
        /// Attempts to retrieve registered expression.
        /// </summary>
        /// <param name="key">Script key.</param>
        /// <param name="expression">Obtained expression.</param>
        /// <returns>True if found.</returns>
        public bool TryGetExpression(string key, [NotNullWhen(true)] out DynValue? expression)
        {
            return _expressions.TryGetValue(key, out expression);
        }

        /// <summary>
        /// Creates a coroutine.
        /// </summary>
        /// <param name="fun">Target function.</param>
        /// <returns>Coroutine.</returns>
        public Coroutine GetCoroutine(DynValue fun)
        {
            return _script.CreateCoroutine(fun).Coroutine;
        }

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        public void RegisterAction(string name, Action action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        public void RegisterAction<T1>(string name, Action<T1> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        public void RegisterAction<T1, T2>(string name, Action<T1, T2> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3>(string name, Action<T1, T2, T3> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4>(string name, Action<T1, T2, T3, T4> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5>(string name, Action<T1, T2, T3, T4, T5> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6>(string name, Action<T1, T2, T3, T4, T5, T6> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="T14">14th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="T14">14th type parameter.</typeparam>
        /// <typeparam name="T15">15th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="T14">14th type parameter.</typeparam>
        /// <typeparam name="T15">15th type parameter.</typeparam>
        /// <typeparam name="T16">16th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<TR>(string name, Func<TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, TR>(string name, Func<T1, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, TR>(string name, Func<T1, T2, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, TR>(string name, Func<T1, T2, T3, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, TR>(string name, Func<T1, T2, T3, T4, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, TR>(string name, Func<T1, T2, T3, T4, T5, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="T14">14th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="T14">14th type parameter.</typeparam>
        /// <typeparam name="T15">15th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="T14">14th type parameter.</typeparam>
        /// <typeparam name="T15">15th type parameter.</typeparam>
        /// <typeparam name="T16">16th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR>(
            string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Runs a function by name.
        /// </summary>
        /// <param name="name">Function to execute.</param>
        public void RunVoidFunctionByName(string name)
        {
            var fn = _script.Globals.Get(name);
            if (Equals(fn, DynValue.Nil)) return;
            _script.Call(fn);
        }

        /// <summary>
        /// Runs a script.
        /// </summary>
        /// <param name="script">Script to execute.</param>
        public void RunVoidScript(DynValue script)
        {
            _script.Call(script);
        }

        /// <summary>
        /// Runs a raw lua script.
        /// </summary>
        /// <param name="script">Script raw contents.</param>
        public void RunVoidScript(string script)
        {
            _script.DoString(script);
        }

        /// <summary>
        /// Runs a function by name with specified arguments.
        /// </summary>
        /// <param name="name">Function to execute.</param>
        /// <param name="args">Arguments to pass.</param>
        public void RunVoidFunctionByName(string name, params object[] args)
        {
            var fn = _script.Globals.Get(name);
            if (Equals(fn, DynValue.Nil)) return;
            _script.Call(fn, args);
        }

        /// <summary>
        /// Runs a script with specified arguments.
        /// </summary>
        /// <param name="script">Script to execute.</param>
        /// <param name="args">Arguments to pass.</param>
        public void RunVoidScript(DynValue script, params object[] args)
        {
            _script.Call(script, args);
        }

        /// <summary>
        /// Runs a raw lua script and gets the result.
        /// </summary>
        /// <param name="script">Script raw contents.</param>
        /// <returns>Retrieved object.</returns>
        public TR? RunScript<TR>(string script)
        {
            var res = _script.DoString(script).ToObject();
            try
            {
                return res is TR || res.GetType().IsValueType ? (TR)res : default;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Runs a script and gets the result.
        /// </summary>
        /// <param name="script">Script to execute.</param>
        /// <typeparam name="TR">Return type.</typeparam>
        /// <returns>Retrieved object.</returns>
        public TR? RunScript<TR>(DynValue script)
        {
            var res = _script.Call(script).ToObject();
            try
            {
                return res is TR || res.GetType().IsValueType ? (TR)res : default;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Runs a script with specified arguments and gets the result.
        /// </summary>
        /// <param name="script">Script to execute.</param>
        /// <param name="args">Arguments to pass.</param>
        /// <typeparam name="TR">Return type.</typeparam>
        /// <returns>Retrieved object.</returns>
        public TR? RunScript<TR>(DynValue script, params object[] args)
        {
            var res = _script.Call(script, args).ToObject();
            try
            {
                return res is TR || res.GetType().IsValueType ? (TR)res : default;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Runs a function by name and gets the result.
        /// </summary>
        /// <param name="name">Function to execute.</param>
        /// <typeparam name="TR">Return type.</typeparam>
        /// <returns>Retrieved object.</returns>
        public TR? RunFunctionByName<TR>(string name)
        {
            var fn = _script.Globals.Get(name);
            if (Equals(fn, DynValue.Nil)) return default;
            var res = _script.Call(fn).ToObject();
            try
            {
                return res is TR || res.GetType().IsValueType ? (TR)res : default;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Runs a function by name with specified arguments and gets the result.
        /// </summary>
        /// <param name="name">Function to execute.</param>
        /// <param name="args">Arguments to pass.</param>
        /// <typeparam name="TR">Return type.</typeparam>
        /// <returns>Retrieved object.</returns>
        public TR? RunFunctionByName<TR>(string name, params object[] args)
        {
            var fn = _script.Globals.Get(name);
            if (Equals(fn, DynValue.Nil)) return default;
            var res = _script.Call(fn, args).ToObject();
            try
            {
                return res is TR || res.GetType().IsValueType ? (TR)res : default;
            }
            catch
            {
                return default;
            }
        }

        #endregion
    }
}

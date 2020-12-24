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
            _script.Globals["mg"] = this;
            _script.Globals["world"] = _world;
            _expressions = new Dictionary<string, DynValue>();

            #region Function registration

            // Manager
            RegisterFunction("mg", Mg);
            RegisterFunction("world", World);

            // Misc convenience

            // Standard members
            RegisterFunction<string, PersonModel?>("person_t", PersonT);
            RegisterFunction<string, SystemModel?>("system_t", SystemT);
            RegisterFunction<string, SystemModel?>("system_a", SystemA);
            RegisterFunction<PersonModel?, SystemModel?>("home", Home);
            RegisterFunction<PersonModel?, string, MissionModel?>("start_mission", StartMission);
            RegisterFunction<PersonModel?, string, bool>("remove_mission", RemoveMission);
            RegisterFunction<SystemModel?, bool>("system_up", SystemUp);
            RegisterFunction<SystemModel?, string, bool>("file_exists", FileExists);
            RegisterFunction<SystemModel?, string, string, bool, bool>("file_contains", FileContains);
            RegisterAction<string>("log", Log);
            RegisterAction<string, int>("log_ex", LogEx);
            RegisterFunction<string, string, PersonModel>("spawn_person", SpawnPerson);
            RegisterFunction<string, string, string, PersonModel>("spawn_person_tagged", SpawnPersonTagged);
            RegisterFunction<PersonModel?, string, string, string, SystemModel?>("spawn_system", SpawnSystem);
            RegisterAction<PersonModel?>("remove_person", RemovePerson);
            RegisterAction<SystemModel?>("remove_system", RemoveSystem);
            RegisterFunction<SystemModel?, string, string, FileModel?>("spawn_file", SpawnFile);
            RegisterAction<FileModel?>("remove_file", RemoveFile);

            // Program, service, and hackscript members
            RunVoidScript(@"function delay(d) coroutine.yield(get_delay(d)) end");
            RegisterFunction<float, YieldToken>("get_delay", GetDelay);

            // Program-only members
            RunVoidScript(@"function write(text) return self.Write(text) end");
            RunVoidScript(@"function flush() return self.Flush() end");
            RunVoidScript(@"function unbind() return self.SignalUnbindProcess() end");

            #endregion
        }

        #region ScriptManager proxy functions

        private ScriptManager Mg() => this;
        private IWorld World() => _world;

        #endregion

        #region Miscellaneous convenience functions

        /*private static Cell2 CvCell2(int x, int y) => new Cell2(x, y);*/

        #endregion

        #region Standard members

        private PersonModel? PersonT(string tag)
        {
            return _world.Model.TaggedPersons.TryGetValue(tag, out var person) ? person : null;
        }

        private SystemModel? SystemT(string tag)
        {
            return _world.Model.TaggedSystems.TryGetValue(tag, out var system) ? system : null;
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

        private bool SystemUp(SystemModel? system)
        {
            if (system == null) return false;
            return system.BootTime <= _world.Time;
        }

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

        private PersonModel SpawnPerson(string name, string username)
        {
            return _world.Spawn.Person(name, username);
        }

        private PersonModel SpawnPersonTagged(string name, string username, string tag)
        {
            return _world.Spawn.Person(name, username, tag);
        }

        private SystemModel? SpawnSystem(PersonModel? owner, string password, string template, string addressRange)
        {
            if (owner == null) return null;
            if (!_world.Templates.SystemTemplates.TryGetValue(template, out var sysTemplate)) return null;
            if (!IPAddressRange.TryParse(addressRange, true, out var range)) return null;
            var (hash, salt) = ServerUtil.HashPassword(password);
            return _world.Spawn.System(sysTemplate, owner, hash, salt, range);
        }

        private void RemovePerson(PersonModel? person)
        {
            if (person == null) return;
            _world.Spawn.RemovePerson(person);
        }

        private void RemoveSystem(SystemModel? system)
        {
            if (system == null) return;
            _world.Spawn.RemoveSystem(system);
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

        private void RemoveFile(FileModel? file)
        {
            if (file == null) return;
            _world.Spawn.RemoveFile(file);
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

        #region Program, service, and hackscript members

        private static YieldToken GetDelay(float delay) => Executable.Delay(delay);

        #endregion

        #region Program-only members

        #endregion

        #region Script functions

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

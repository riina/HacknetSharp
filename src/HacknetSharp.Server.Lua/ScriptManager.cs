using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using HacknetSharp.Events.Server;
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
    public partial class ScriptManager : IWorldPlugin
    {
        static ScriptManager() => ScriptUtil.Init();

        private readonly Script _script;
        private readonly Dictionary<string, DynValue> _expressions;
        private readonly HashSet<string> _globals;
        private readonly HashSet<MissionModel> _tmpMissions;
        private readonly HashSet<SystemModel> _tmpSystems;
        private readonly HashSet<CronModel> _tmpTasks;
        private readonly ConcurrentQueue<QueuedMission> _missionQueue;
        private readonly Dictionary<string, DynValue> _scriptFile;
        private readonly Dictionary<string, DynValue> _scriptMissionStart;
        private readonly Dictionary<string, Dictionary<int, DynValue>> _scriptMissionGoal;
        private readonly Dictionary<string, Dictionary<int, DynValue>> _scriptMissionNext;
        private readonly Dictionary<CronModel, DynValue> _cronTasks;
        private IWorld? _world;

        /// <summary>
        /// Creates a new instance of <see cref="ScriptManager"/>.
        /// </summary>
        public ScriptManager()
        {
            _script = new Script(CoreModules.Preset_SoftSandbox);
            _expressions = new Dictionary<string, DynValue>();
            _globals = new HashSet<string>();
            _tmpMissions = new HashSet<MissionModel>();
            _tmpSystems = new HashSet<SystemModel>();
            _tmpTasks = new HashSet<CronModel>();
            _missionQueue = new ConcurrentQueue<QueuedMission>();
            _scriptFile = new Dictionary<string, DynValue>();
            _scriptMissionStart = new Dictionary<string, DynValue>();
            _scriptMissionGoal = new Dictionary<string, Dictionary<int, DynValue>>();
            _scriptMissionNext = new Dictionary<string, Dictionary<int, DynValue>>();
            _cronTasks = new Dictionary<CronModel, DynValue>();
            //SetGlobal("ScriptManager", this);
            var defaultGlobals = new Dictionary<string, object>();
            LoadGlobalsFromObject(this, defaultGlobals);
            AddGlobals(defaultGlobals);
        }

        void IWorldPlugin.Initialize(IWorld world)
        {
            _world = world;
            SetGlobal("world", world);
            AddGlobals(new BaseGlobals(this, world).MyGlobals);
            foreach (var system in _world.Model.Systems)
            {
                foreach (var cron in system.Tasks) AssignCronTask(cron, EvaluateScript(GetWrappedLua(cron.Content, true)));
            }
            foreach (var (missionPath, mission) in _world.Templates.MissionTemplates)
            {
                if (!string.IsNullOrWhiteSpace(mission.Start))
                    RegisterScriptMissionStart(missionPath, mission.Start);
                if (mission.Goals != null)
                    for (int i = 0; i < mission.Goals.Count; i++)
                    {
                        string goal = mission.Goals[i];
                        if (!string.IsNullOrWhiteSpace(goal))
                            RegisterScriptMissionGoal(missionPath, i, goal);
                    }

                if (mission.Outcomes != null)
                    for (int i = 0; i < mission.Outcomes.Count; i++)
                    {
                        var outcome = mission.Outcomes[i];
                        if (!string.IsNullOrWhiteSpace(outcome.Next))
                            RegisterScriptMissionNext(missionPath, i, outcome.Next);
                    }
            }

            foreach (var (name, fun) in _world.Templates.LuaSources)
            {
                using var fs = fun();
                RegisterScriptFile(name, fs);
            }
        }

        void IWorldPlugin.Tick()
        {
            if (_world == null) return;
            // Missions
            while (_missionQueue.TryDequeue(out var dequeued))
            {
                try
                {
                    StartMission(dequeued.Person, dequeued.MissionPath, dequeued.CampaignKey);
                }
                catch (Exception e)
                {
                    _world.Logger.LogWarning("Exception thrown while starting queued mission: {Exception}", e);
                }
            }
            _tmpMissions.Clear();
            _tmpMissions.UnionWith(_world.Model.ActiveMissions);
            foreach (var mission in _tmpMissions)
            {
                SetGlobal("self", mission);
                SetGlobal("me", mission.Person);
                SetGlobal("key", mission.CampaignKey);
                try
                {
                    if (!ProcessMission(mission))
                        _world.Spawn.RemoveMission(mission);
                }
                catch (Exception e)
                {
                    _world.Logger.LogWarning("Exception thrown while processing mission {Mission}:\n{Exception}",
                        mission.Template, e);
                    _world.Spawn.RemoveMission(mission);
                }
                finally
                {
                    ClearGlobal("self");
                    ClearGlobal("me");
                    ClearGlobal("key");
                }
            }
            // Tasks

            _tmpSystems.Clear();
            _tmpSystems.UnionWith(_world.Model.Systems);
            foreach (var system in _tmpSystems)
            {
                if (system.BootTime > _world.Time) continue;
                _tmpTasks.Clear();
                _tmpTasks.UnionWith(system.Tasks);
                foreach (var task in _tmpTasks)
                {
                    if (task.End > _world.Time)
                        RemoveCron(task);
                    if (task.LastRunAt + task.Delay < _world.Time)
                    {
                        SetGlobal("self", task.System);
                        SetGlobal("system", task.System);

                        try
                        {
                            if (!TryGetCronTask(task, out var cronTask))
                            {
                                cronTask = EvaluateScript(GetWrappedLua(task.Content, true));
                                AssignCronTask(task, cronTask);
                            }
                            RunVoidScript(cronTask);
                        }
                        catch (Exception e)
                        {
                            _world.Logger.LogWarning(
                                "Exception thrown while processing task on system {Id} with contents:\n{Content}\n{Exception}",
                                task.System.Key, task.Content, e);
                            RemoveCron(task);
                        }
                        finally
                        {
                            ClearGlobal("self");
                            ClearGlobal("system");
                        }

                        task.LastRunAt += task.Delay;
                        _world.Database.Update(task);
                    }
                }
            }
        }

        /// <inheritdoc />
        public bool TryProvideProgram(string command, string[] line, out (Program, ProgramInfoAttribute?, string[]) result)
        {
            if (line[0].EndsWith(".program.script.lua") && TryGetScriptFile(line[0], out var script))
            {
                result = (new LuaProgram(GetCoroutine(script)), null, line);
                return true;
            }
            result = default;
            return false;
        }

        internal void AssignCronTask(CronModel model, DynValue task)
        {
            _cronTasks[model] = task;
        }

        internal bool TryGetCronTask(CronModel model, [NotNullWhen(true)] out DynValue? task)
        {
            return _cronTasks.TryGetValue(model, out task);
        }

        internal void RemoveCronTask(CronModel model)
        {
            _cronTasks.Remove(model);
        }

        private void RemoveCron(CronModel model)
        {
            _world!.Spawn.RemoveCron(model);
            RemoveCronTask(model);
        }

        /// <inheritdoc />
        public bool TryProvideService(string command, string[] line, out (Service, ServiceInfoAttribute?, string[]) result)
        {
            if (line[0].EndsWith(".service.script.lua") && TryGetScriptFile(line[0], out var script))
            {
                result = (new LuaService(GetCoroutine(script)), null, line);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Starts a mission for the specified person.
        /// </summary>
        /// <param name="person">Target person.</param>
        /// <param name="missionPath">Mission template path.</param>
        /// <param name="campaignKey">Campaign key.</param>
        /// <returns>Started mission</returns>
        public MissionModel? StartMission(PersonModel person, string missionPath, Guid campaignKey)
        {
            if (_world == null) return null;
            if (!_world.Templates.MissionTemplates.TryGetValue(missionPath, out var template))
                return null;
            var mission = _world.Spawn.Mission(missionPath, person, campaignKey);
            mission.Data = template;
            if (!string.IsNullOrWhiteSpace(template.Start))
            {
                SetGlobal("self", mission);
                SetGlobal("me", person);
                SetGlobal("key", campaignKey);
                try
                {
                    if (TryGetScriptMissionStart(missionPath, out var script))
                        RunVoidScript(script);
                }
                finally
                {
                    ClearGlobal("self");
                    ClearGlobal("me");
                    ClearGlobal("key");
                }
            }

            if (person.User is { } user)
                foreach (var output in user.Outputs)
                {
                    output.WriteEventSafe(new AlertEvent
                    {
                        Alert = AlertEvent.Kind.System,
                        Header = "New Mission",
                        Body =
                            $"<< {template.Campaign} - {template.Title} >>\n\n{template.Message}"
                    });
                    output.FlushSafeAsync();
                }

            _world.Logger.LogInformation("Successfully started mission {Path} for person {Id}", mission.Template,
                mission.Person.Key);
            return mission;
        }

        /// <summary>
        /// Queues a mission for the specified person.
        /// </summary>
        /// <param name="person">Target person.</param>
        /// <param name="missionPath">Mission template path.</param>
        /// <param name="campaignKey">Campaign key.</param>
        public void QueueMission(PersonModel person, string missionPath, Guid campaignKey)
        {
            _missionQueue.Enqueue(new QueuedMission(person, missionPath, campaignKey));
        }

        private bool ProcessMission(MissionModel mission)
        {
            if (_world == null) return false;
            Span<byte> flags = stackalloc byte[8];
            if (!_world.Templates.MissionTemplates.TryGetValue(mission.Template, out var m)) return false;
            if (m.Goals is not { } goals || m.Outcomes is not { } outcomes) return false;
            long original = mission.Flags;
            BinaryPrimitives.WriteInt64BigEndian(flags, original);
            for (int i = 0; i < goals.Count; i++)
            {
                if (GetFlag(flags, i)) continue;
                if (TryGetScriptMissionGoal(mission.Template, i, out var script))
                {
                    bool? res = RunScript<bool?>(script);
                    if (res != null && res.Value) SetFlag(flags, i, true);
                }
            }

            bool end = false;
            for (int i = 0; i < outcomes.Count; i++)
            {
                var outcome = outcomes[i];
                bool fail = false;
                if (outcome.Goals == null || outcome.Goals.Count == 0)
                {
                    for (int j = 0; j < goals.Count; j++)
                        if (!GetFlag(flags, j))
                        {
                            fail = true;
                            break;
                        }
                }
                else
                {
                    foreach (int j in outcome.Goals)
                        if (!GetFlag(flags, j))
                        {
                            fail = true;
                            break;
                        }
                }

                if (!fail)
                {
                    if (mission.Person.User is { } user)
                        foreach (var output in user.Outputs)
                        {
                            output.WriteEventSafe(new AlertEvent
                            {
                                Alert = AlertEvent.Kind.System,
                                Header = "MISSION COMPLETE",
                                Body =
                                    $"<< {m.Campaign} - {m.Title} >>"
                            });
                            output.FlushSafeAsync();
                        }

                    _world.Logger.LogInformation("Successfully finished mission {Path} for person {Id}", mission.Template,
                        mission.Person.Key);
                    if (TryGetScriptMissionNext(mission.Template, i, out var script))
                        RunVoidScript(script);
                    _world.Spawn.RemoveMission(mission);
                    end = true;
                    break;
                }
            }

            if (!end)
            {
                long changed = BinaryPrimitives.ReadInt64BigEndian(flags);
                if (changed != original)
                {
                    mission.Flags = changed;
                    _world.Database.Update(mission);
                }
            }

            return true;
        }

        private static bool GetFlag(Span<byte> flags, int i)
        {
            return ((flags[i / 8] >> (i % 8)) & 1) != 0;
        }

        private static void SetFlag(Span<byte> flags, int i, bool value)
        {
            if (value)
                flags[i / 8] |= (byte)(1 << (i % 8));
            else
                flags[i / 8] &= (byte)~(1 << (i % 8));
        }

        private void RegisterScriptFile(string name, Stream stream)
        {
            _scriptFile[name] = EvaluateScript(GetWrappedLua(stream, true));
        }

        internal bool TryGetScriptFile(string name, [NotNullWhen(true)] out DynValue? script)
        {
            if (_scriptFile.TryGetValue(name, out script)) return true;
            script = null;
            return false;
        }

        private void RegisterScriptMissionStart(string missionPath, string content)
        {
            _scriptMissionStart[missionPath] = EvaluateScript(GetWrappedLua(content, true));
        }

        private bool TryGetScriptMissionStart(string missionPath, [NotNullWhen(true)] out DynValue? script)
        {
            if (_scriptMissionStart.TryGetValue(missionPath, out script)) return true;
            script = null;
            return false;
        }

        private void RegisterScriptMissionGoal(string missionPath, int index, string content)
        {
            if (!_scriptMissionGoal.TryGetValue(missionPath, out var dict))
                _scriptMissionGoal[missionPath] = dict = new Dictionary<int, DynValue>();
            dict[index] = EvaluateScript(GetWrappedLua(content, false));
        }

        private bool TryGetScriptMissionGoal(string missionPath, int index, [NotNullWhen(true)] out DynValue? script)
        {
            if (_scriptMissionGoal.TryGetValue(missionPath, out var dict) && dict.TryGetValue(index, out script))
                return true;

            script = null;
            return false;
        }

        private void RegisterScriptMissionNext(string missionPath, int index, string content)
        {
            if (!_scriptMissionNext.TryGetValue(missionPath, out var dict))
                _scriptMissionNext[missionPath] = dict = new Dictionary<int, DynValue>();
            dict[index] = EvaluateScript(GetWrappedLua(content, true));
        }

        private bool TryGetScriptMissionNext(string missionPath, int index, [NotNullWhen(true)] out DynValue? script)
        {
            if (_scriptMissionNext.TryGetValue(missionPath, out var dict) && dict.TryGetValue(index, out script))
                return true;

            script = null;
            return false;
        }

        private static string GetWrappedLua(string body, bool isVoid) =>
            isVoid
                ? $"return function() {body} end"
                : $"return function() return {body} end";

        private static string GetWrappedLua(Stream body, bool isVoid)
        {
            using var sr = new StreamReader(body);
            return GetWrappedLua(sr.ReadToEnd(), isVoid);
        }
    }
}

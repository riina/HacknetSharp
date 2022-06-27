using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HacknetSharp.Server.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Base server implementation.
    /// </summary>
    public partial class ServerBase
    {
        private const float SaveDelaySeconds = 10;

        internal IEnumerable<Type> PluginTypes => _pluginTypes;

        private readonly HashSet<Type> _programTypes;
        private readonly HashSet<Type> _serviceTypes;
        private readonly HashSet<Type> _pluginTypes;
        private readonly CountdownEvent _countdown;
        private readonly AutoResetEvent _op;
        private readonly Queue<ProgramContext> _inputQueue;
        private readonly List<ProgramContext> _inputProcessing;
        private readonly AutoResetEvent _queueOp;
        private long _ms;
        private float _saveElapsed;
        private long _lastMs;
        private bool _databaseConfigured;
        private bool _defaultWorldConfigured;

        /// <summary>
        /// Current lifecycle state.
        /// </summary>
        protected LifecycleState State;

        /// <summary>
        /// Worlds.
        /// </summary>
        public Dictionary<Guid, World> Worlds { get; }

        /// <summary>
        /// Default world.
        /// </summary>
        public World DefaultWorld { get; private set; } = null!;

        /// <summary>
        /// Programs.
        /// </summary>
        public Dictionary<string, (Func<Program>, ProgramInfoAttribute)> Programs { get; }

        /// <summary>
        /// Intrinsic programs.
        /// </summary>
        public Dictionary<string, (Func<Program>, ProgramInfoAttribute)> IntrinsicPrograms { get; }

        /// <summary>
        /// Services.
        /// </summary>
        public Dictionary<string, (Func<Service>, ServiceInfoAttribute)> Services { get; }

        /// <summary>
        /// Templates.
        /// </summary>
        public TemplateGroup Templates { get; }

        /// <summary>
        /// Database.
        /// </summary>
        public IServerDatabase Database { get; private set; } = null!;

        /// <summary>
        /// Spawn manager.
        /// </summary>
        public Spawn Spawn { get; private set; } = null!;

        /// <summary>
        /// Message of the day.
        /// </summary>
        public string? Motd { get; }

        /// <summary>
        /// Logger.
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ServerBase"/>.
        /// </summary>
        /// <param name="config">Configuration.</param>
        protected ServerBase(ServerConfigBase config)
        {
            Worlds = new Dictionary<Guid, World>();
            Templates = config.Templates;
            Logger = config.Logger ?? NullLogger.Instance;
            Motd = config.Motd;
            _programTypes = new HashSet<Type>(ServerUtil.DefaultPrograms);
            _programTypes.UnionWith(config.Programs);
            _serviceTypes = new HashSet<Type>(ServerUtil.DefaultServices);
            _serviceTypes.UnionWith(config.Services);
            _pluginTypes = new HashSet<Type>(ServerUtil.DefaultPlugins);
            _pluginTypes.UnionWith(config.Plugins);
            Programs = new Dictionary<string, (Func<Program>, ProgramInfoAttribute)>();
            IntrinsicPrograms = new Dictionary<string, (Func<Program>, ProgramInfoAttribute)>();
            Services = new Dictionary<string, (Func<Service>, ServiceInfoAttribute)>();
            _countdown = new CountdownEvent(1);
            _op = new AutoResetEvent(true);
            _inputQueue = new Queue<ProgramContext>();
            _inputProcessing = new List<ProgramContext>();
            _queueOp = new AutoResetEvent(true);
            State = LifecycleState.NotStarted;
        }

        /// <summary>
        /// Configures database.
        /// </summary>
        /// <param name="database">Database to use.</param>
        protected void ConfigureDatabase(IServerDatabase database)
        {
            Database = database;
            Spawn = new Spawn(Database);
            _databaseConfigured = true;
        }

        /// <summary>
        /// Configures default world.
        /// </summary>
        /// <param name="defaultWorld">Default world.</param>
        protected void ConfigureDefaultWorld(World defaultWorld)
        {
            DefaultWorld = defaultWorld;
            _defaultWorldConfigured = true;
        }

        private static readonly MethodInfo s_getConstructorDelegate =
            typeof(ServerBase).GetMethod(nameof(GetConstructorDelegate),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;

        private void StartInternal()
        {
            if (!_databaseConfigured) throw new InvalidOperationException("Database not configured prior to start call");
            if (!_defaultWorldConfigured) throw new InvalidOperationException("Default world not configured prior to start call");
            Util.TriggerState(_op, LifecycleState.NotStarted, LifecycleState.NotStarted, LifecycleState.Starting,
                ref State);
            try
            {
                foreach (var type in _programTypes)
                {
                    if (type.GetCustomAttribute(typeof(IgnoreRegistrationAttribute)) != null) continue;
                    if (type.GetCustomAttribute(typeof(ProgramInfoAttribute)) is not ProgramInfoAttribute info)
                    {
                        Logger.LogWarning(
                            $"{type.FullName} supplied as program but did not have {nameof(ProgramInfoAttribute)}");
                        continue;
                    }

                    var func = (Func<Program>)(s_getConstructorDelegate.MakeGenericMethod(type, typeof(Program))
                                                   .Invoke(null, Array.Empty<object>()) ??
                                               throw new ApplicationException(
                                                   $"{type.FullName} supplied as program but failed to get delegate"));
                    Programs.Add(info.ProgCode, (func, info));
                    if (info.Intrinsic)
                        IntrinsicPrograms.Add(info.Name, (func, info));
                }

                foreach (var type in _serviceTypes)
                {
                    if (type.GetCustomAttribute(typeof(IgnoreRegistrationAttribute)) != null) continue;
                    if (type.GetCustomAttribute(typeof(ServiceInfoAttribute)) is not ServiceInfoAttribute info)
                    {
                        Logger.LogWarning(
                            $"{type.FullName} supplied as service but did not have {nameof(ServiceInfoAttribute)}");
                        continue;
                    }

                    var func = (Func<Service>)(s_getConstructorDelegate.MakeGenericMethod(type, typeof(Service))
                                                   .Invoke(null, Array.Empty<object>()) ??
                                               throw new ApplicationException(
                                                   $"{type.FullName} supplied as service but failed to get delegate"));
                    Services.Add(info.ProgCode, (func, info));
                }
            }
            catch
            {
                Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Disposed,
                    ref State);
                throw;
            }

            Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Active, ref State);
            StartListening();
        }

        /// <summary>
        /// Starts listening for connections.
        /// </summary>
        protected virtual void StartListening()
        {
        }

        private static Func<TBase> GetConstructorDelegate<T, TBase>() where T : TBase, new() => () => new T();

        private void UpdateMain(float deltaTime)
        {
            _queueOp.WaitOne();
            _inputProcessing.AddRange(_inputQueue);
            _inputQueue.Clear();
            _queueOp.Set();
            foreach (var context in _inputProcessing)
                context.World.ExecuteCommand(context);
            _inputProcessing.Clear();
            foreach (var world in Worlds.Values)
            {
                world.PreviousTime = world.Model.Now;
                world.Time = world.PreviousTime + deltaTime;
                world.Model.Now = world.Time;
                try
                {
                    world.Tick();
                }
                catch (Exception e)
                {
                    Logger.LogWarning("Unhandled thrown while updating world {Id}:\n{Exception}",
                        world.Model.Key, e);
                }
            }
        }

        private bool CheckSave(float deltaTime)
        {
            float saveTime = _saveElapsed + deltaTime;
            if (saveTime >= SaveDelaySeconds)
            {
                Logger.LogInformation("Database save {Time}", DateTime.Now);
                _saveElapsed = (saveTime - SaveDelaySeconds) % SaveDelaySeconds;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if default system for user is available.
        /// </summary>
        /// <param name="userModel">User.</param>
        /// <returns>True if available.</returns>
        public bool DefaultSystemAvailable(UserModel userModel)
        {
            if (!Worlds.TryGetValue(userModel.ActiveWorld, out var world)) return false;
            var worldModel = world.Model;
            var person = userModel.Identities.FirstOrDefault(p => p.World == worldModel);
            if (person == null) return false;
            if (!world.TryGetSystem(person.DefaultSystem, out var system)) return false;
            return system.BootTime <= world.Time;
        }

        /// <summary>
        /// Queues connect command.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="user">User.</param>
        /// <param name="operationId">Operation.</param>
        /// <param name="conWidth">Console width.</param>
        /// <returns>True for connection success.</returns>
        public bool QueueConnectCommand(IPersonContext context, UserModel user, Guid operationId, int conWidth)
        {
            _queueOp.WaitOne();
            try
            {
                if (!Worlds.TryGetValue(user.ActiveWorld, out var world))
                {
                    world = DefaultWorld;
                    user.ActiveWorld = world.Model.Key;
                    Database.Update(user);
                }

                if (!DefaultSystemAvailable(user))
                {
                    context.WriteEventSafe(Program.Output("Your default system is not available, it may be restarting.\n"));
                    _ = context.FlushSafeAsync();
                    return false;
                }

                var person = context.GetPerson(world);

                _inputQueue.Enqueue(ServerUtil.InitTentativeProgramContext(world, operationId, context, person, "",
                    invocationType: person.StartedUp
                        ? ProgramContext.InvocationType.Connect
                        : ProgramContext.InvocationType.StartUp, conWidth: conWidth));
                return true;
            }
            finally
            {
                _queueOp.Set();
            }
        }

        /// <summary>
        /// Queues command.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="user">User.</param>
        /// <param name="operationId">Operation.</param>
        /// <param name="conWidth">Console width.</param>
        /// <param name="command">Command.</param>
        public void QueueCommand(IPersonContext context, UserModel user, Guid operationId, int conWidth, string command)
        {
            _queueOp.WaitOne();
            try
            {
                if (!Worlds.TryGetValue(user.ActiveWorld, out var world))
                {
                    world = DefaultWorld;
                    user.ActiveWorld = world.Model.Key;
                    Database.Update(user);
                }

                _inputQueue.Enqueue(ServerUtil.InitTentativeProgramContext(world, operationId, context, context.GetPerson(world), command, conWidth: conWidth));
            }
            finally
            {
                _queueOp.Set();
            }
        }

        /// <summary>
        /// Attempts to increment a countdown based on an expectation of the current lifecycle state.
        /// </summary>
        /// <param name="min">Minimum state.</param>
        /// <param name="max">Maximum state.</param>
        /// <returns>True if state limits were met and countdown was incremented.</returns>
        protected bool TryIncrementCountdown(LifecycleState min, LifecycleState max) =>
            Util.TryIncrementCountdown(_op, _countdown, State, min, max);

        /// <summary>
        /// Decrements a countdown, synchronized using a concurrency event.
        /// </summary>
        protected void DecrementCountdown() => Util.DecrementCountdown(_op, _countdown);
    }
}

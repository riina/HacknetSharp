using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp.Server.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Base server implementation.
    /// </summary>
    public class ServerBase
    {
        private readonly HashSet<Type> _programTypes;
        private readonly HashSet<Type> _serviceTypes;
        private readonly CountdownEvent _countdown;
        private readonly AutoResetEvent _op;
        private readonly Queue<ProgramContext> _inputQueue;
        private readonly List<ProgramContext> _inputProcessing;
        private readonly AutoResetEvent _queueOp;
        private long _ms;
        private long _lastSave;
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

        private static readonly MethodInfo _getConstructorDelegate =
            typeof(ServerBase).GetMethod(nameof(GetConstructorDelegate),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;

        /// <summary>
        /// Starts instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when improperly configured.</exception>
        /// <exception cref="ApplicationException">Thrown for problem in startup.</exception>
        public Task Start()
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

                    var func = (Func<Program>)(_getConstructorDelegate.MakeGenericMethod(type, typeof(Program))
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

                    var func = (Func<Service>)(_getConstructorDelegate.MakeGenericMethod(type, typeof(Service))
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
            return UpdateAsync();
        }

        /// <summary>
        /// Starts listening for connections.
        /// </summary>
        protected virtual void StartListening()
        {
        }

        private static Func<TBase> GetConstructorDelegate<T, TBase>() where T : TBase, new() => () => new T();

        private async Task UpdateAsync()
        {
            long ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            _ms = ms;
            _lastSave = ms;
            _lastMs = ms;
            while (TryIncrementCountdown(LifecycleState.Active, LifecycleState.Active))
            {
                try
                {
                    await UpdateCoreAsync();
                }
                finally
                {
                    DecrementCountdown();
                }
            }
        }

        /// <summary>
        /// Core update.
        /// </summary>
        protected virtual async Task UpdateCoreAsync()
        {
            const int tickMs = 10;
            const int saveDelayMs = 10 * 1000;
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
                world.Time = world.PreviousTime + (_ms - _lastMs) / 1000.0;
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

            long ms2 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            long ms3;
            if (_lastSave + saveDelayMs < ms2)
            {
                Logger.LogInformation("Database saving {Time}", DateTime.Now);
                _lastSave = ms2;
                await Database.SyncAsync().Caf();
                ms3 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
            else
                ms3 = ms2;

            await Task.Delay((int)Math.Min(tickMs, Math.Max(0, tickMs - (ms3 - _ms)))).Caf();
            _lastMs = _ms;
            _ms = ms3;
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
        /// Disconnects active connections.
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual Task DisconnectConnectionsAsync() => Task.CompletedTask;

        /// <summary>
        /// Waits for connection task to end.
        /// </summary>
        /// <returns>Task.</returns>
        protected virtual Task WaitForStopListening() => Task.CompletedTask;

        /// <summary>
        /// Disposes instance.
        /// </summary>
        public async Task DisposeAsync()
        {
            if (State == LifecycleState.Disposed) return;
            Util.RequireState(State, LifecycleState.Starting, LifecycleState.Active);
            while (State != LifecycleState.Active) await Task.Delay(100).Caf();
            Util.TriggerState(_op, LifecycleState.Active, LifecycleState.Active, LifecycleState.Dispose, ref State);
            await DisconnectConnectionsAsync();

            await Task.Run(() =>
            {
                _op.WaitOne();
                _countdown.Signal();
                _op.Set();
                _countdown.Wait();
            }).Caf();
            await WaitForStopListening();
            Logger.LogInformation("Committing database on close");
            try
            {
                await Database.SyncAsync();
            }
            catch (Exception e)
            {
                Logger.LogWarning("Database commit failed with an exception:\n{Exception}", e);
            }
            finally
            {
                Util.TriggerState(_op, LifecycleState.Dispose, LifecycleState.Dispose, LifecycleState.Disposed,
                    ref State);
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

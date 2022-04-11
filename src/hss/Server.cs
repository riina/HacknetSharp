using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp;
using HacknetSharp.Events.Server;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace hss
{
    public class Server
    {
        private static readonly CultureInfo s_ic = CultureInfo.InvariantCulture;

        private readonly HashSet<Type> _programTypes;
        private readonly HashSet<Type> _serviceTypes;
        private readonly CountdownEvent _countdown;
        private readonly AutoResetEvent _op;
        private readonly ConcurrentDictionary<Guid, HostConnection> _connections;
        private readonly Queue<ProgramContext> _inputQueue;
        private readonly List<ProgramContext> _inputProcessing;
        private readonly AutoResetEvent _queueOp;
        private LifecycleState _state;

        private readonly TcpListener _connectListener;
        private Task? _connectTask;
        internal X509Certificate Cert { get; }
        internal AccessController AccessController { get; }
        public Dictionary<Guid, World> Worlds { get; }
        public World DefaultWorld { get; }
        public Dictionary<string, (Func<Program>, ProgramInfoAttribute)> Programs { get; }
        public Dictionary<string, (Func<Program>, ProgramInfoAttribute)> IntrinsicPrograms { get; }
        public Dictionary<string, (Func<Service>, ServiceInfoAttribute)> Services { get; }
        public TemplateGroup Templates { get; }
        public ServerDatabase Database { get; }
        public Spawn Spawn { get; }
        public string? Motd { get; }
        public ILogger Logger { get; }

        protected internal Server(ServerConfig config)
        {
            Cert = config.Certificate ?? throw new ArgumentException(
                $"{nameof(ServerConfig.Certificate)} not specified");
            var factory = config.StorageContextFactory ??
                          throw new ArgumentException($"{nameof(ServerConfig.StorageContextFactory)} not specified");
            var context = factory.CreateDbContext(Array.Empty<string>());
            Database = new ServerDatabase(context);
            AccessController = new AccessController(this);
            Worlds = new Dictionary<Guid, World>();
            Templates = config.Templates;
            Spawn = new Spawn(Database);
            Logger = config.Logger ?? NullLogger.Instance;
            World? defaultWorld = null;
            foreach (var w in context.Set<WorldModel>())
            {
                var world = new World(this, w, Database);
                Worlds[w.Key] = world;
                if (w.Name.Equals(config.DefaultWorld)) defaultWorld = world;
            }

            DefaultWorld = defaultWorld ?? throw new ApplicationException("No world matching name found");
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
            _connectListener = new TcpListener(IPAddress.Any, config.Port);
            _connections = new ConcurrentDictionary<Guid, HostConnection>();
            _inputQueue = new Queue<ProgramContext>();
            _inputProcessing = new List<ProgramContext>();
            _queueOp = new AutoResetEvent(true);
            _state = LifecycleState.NotStarted;
        }


        private async Task RunConnectListener()
        {
            try
            {
                while (TryIncrementCountdown(LifecycleState.Active, LifecycleState.Active))
                {
                    try
                    {
                        var connection = new HostConnection(this, await _connectListener.AcceptTcpClientAsync().Caf());
                        _connections.TryAdd(connection.Id, connection);
                    }
                    catch (IOException ioe)
                    {
                        Logger.LogWarning("Connection listener threw an IO exception:\n{Exception}", ioe);
                        return;
                    }
                    catch (SocketException se)
                    {
                        if (se.SocketErrorCode == SocketError.OperationAborted)
                            return;
                        else
                            Logger.LogWarning("Connection listener threw a socket exception:\n{Exception}", se);
                    }
                    finally
                    {
                        Util.DecrementCountdown(_op, _countdown);
                    }
                }
            }
            finally
            {
                if (_state >= LifecycleState.Dispose)
                    Logger.LogInformation("Connection listener is offline");
                else
                    Logger.LogWarning("Connection listener has closed sooner than expected");
            }
        }

        private static readonly MethodInfo _getConstructorDelegate =
            typeof(Server).GetMethod(nameof(GetConstructorDelegate),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)!;

        public Task Start()
        {
            Util.TriggerState(_op, LifecycleState.NotStarted, LifecycleState.NotStarted, LifecycleState.Starting,
                ref _state);
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
                    ref _state);
                throw;
            }

            Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Active, ref _state);

            _connectListener.Start();
            _connectTask = RunConnectListener();
            return UpdateAsync();
        }

        private static Func<TBase> GetConstructorDelegate<T, TBase>() where T : TBase, new() => () => new T();

        private async Task UpdateAsync()
        {
            long ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            const int tickMs = 10;
            const int saveDelayMs = 10 * 1000;
            long lastSave = ms;
            long lastMs = ms;
            while (TryIncrementCountdown(LifecycleState.Active, LifecycleState.Active))
            {
                try
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
                        world.Time = world.PreviousTime + (ms - lastMs) / 1000.0;
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
                    if (lastSave + saveDelayMs < ms2)
                    {
                        Logger.LogInformation("Database saving {Time}", DateTime.Now);
                        lastSave = ms2;
                        await Database.SyncAsync().Caf();
                        ms3 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    }
                    else
                        ms3 = ms2;

                    await Task.Delay((int)Math.Min(tickMs, Math.Max(0, tickMs - (ms3 - ms)))).Caf();
                    lastMs = ms;
                    ms = ms3;
                }
                catch (DbUpdateConcurrencyException e)
                {
                    var sb = new StringBuilder();
                    foreach (var x in e.Entries)
                    {
                        sb.AppendLine(s_ic, $"{x.Entity}");
                        switch (x.Entity)
                        {
                            case FileModel y:
                                sb.AppendLine(s_ic, $"[{y.Path}] [{y.Name}]");
                                sb.AppendLine("Current:");
                                foreach (var z in x.CurrentValues.Properties)
                                    sb.AppendLine(s_ic, $"{z.Name} // {z} // [{x.CurrentValues[z]}] vs [{x.OriginalValues[z]}]");
                                break;
                        }
                    }
                    Logger.LogError(
                        $"{nameof(DbUpdateConcurrencyException)} thrown in server loop:\n{{Exception}}\nDetails:\n{{Information}}",
                        e, sb.ToString());

                    return;
                }
                finally
                {
                    DecrementCountdown();
                }
            }
        }

        public bool DefaultSystemAvailable(UserModel userModel)
        {
            if (!Worlds.TryGetValue(userModel.ActiveWorld, out var world)) return false;
            var worldModel = world.Model;
            var person = userModel.Identities.FirstOrDefault(p => p.World == worldModel);
            if (person == null) return false;
            if (!world.TryGetSystem(person.DefaultSystem, out var system)) return false;
            return system.BootTime <= world.Time;
        }

        public bool QueueConnectCommand(HostConnection context, UserModel user, Guid operationId, int conWidth)
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
                    context.WriteEventSafe(
                        Program.Output("Your default system is not available, it may be restarting.\n"));
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

        public void QueueCommand(HostConnection context, UserModel user, Guid operationId, int conWidth, string command)
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

                _inputQueue.Enqueue(ServerUtil.InitTentativeProgramContext(world, operationId, context,
                    context.GetPerson(world), command, conWidth: conWidth));
            }
            finally
            {
                _queueOp.Set();
            }
        }

        public async Task DisposeAsync()
        {
            if (_state == LifecycleState.Disposed) return;
            Util.RequireState(_state, LifecycleState.Starting, LifecycleState.Active);
            while (_state != LifecycleState.Active) await Task.Delay(100).Caf();
            Util.TriggerState(_op, LifecycleState.Active, LifecycleState.Active, LifecycleState.Dispose, ref _state);
            var connectionIds = _connections.Keys;
            _connectListener.Stop();
            foreach (var id in connectionIds)
            {
                Logger.LogInformation("Disconnecting connection {Id} for server dispose", id);
                await DisconnectConnectionAsync(id);
            }

            await Task.Run(() =>
            {
                _op.WaitOne();
                _countdown.Signal();
                _op.Set();
                _countdown.Wait();
            }).Caf();
            await _connectTask!;
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
                    ref _state);
            }
        }

        public bool HasConnection(string userName) => _connections.Any(c => c.Value.UserName == userName);

        internal void SelfRemoveConnection(Guid id) => _connections.TryRemove(id, out _);

        private async Task DisconnectConnectionAsync(Guid id)
        {
            if (_connections.TryRemove(id, out var connection))
            {
                try
                {
                    connection.WriteEventSafe(new ServerDisconnectEvent { Reason = "Server shutting down." });
                    await connection.FlushSafeAsync();
                    connection.Dispose();
                }
                catch
                {
                    // ignored
                }
            }
        }

        internal bool TryIncrementCountdown(LifecycleState min, LifecycleState max) =>
            Util.TryIncrementCountdown(_op, _countdown, _state, min, max);

        internal void DecrementCountdown() => Util.DecrementCountdown(_op, _countdown);
    }
}

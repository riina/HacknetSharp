using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp;
using HacknetSharp.Events.Server;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace hss
{
    public class Server
    {
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
        public Dictionary<string, (Program, ProgramInfoAttribute)> Programs { get; }
        public Dictionary<string, (Program, ProgramInfoAttribute)> IntrinsicPrograms { get; }
        public Dictionary<string, (Service, ServiceInfoAttribute)> Services { get; }
        public TemplateGroup Templates { get; }
        public ServerDatabase Database { get; }
        public Spawn Spawn { get; }
        public string? Motd { get; }

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
            Programs = new Dictionary<string, (Program, ProgramInfoAttribute)>();
            IntrinsicPrograms = new Dictionary<string, (Program, ProgramInfoAttribute)>();
            Services = new Dictionary<string, (Service, ServiceInfoAttribute)>();
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
                    catch (IOException)
                    {
                        return;
                    }
                    catch (SocketException)
                    {
                        return;
                    }
                    finally
                    {
                        Util.DecrementCountdown(_op, _countdown);
                    }
                }
            }
            finally
            {
                Console.WriteLine("[[Connection listener is offline.]]");
            }
        }

        public Task Start()
        {
            Util.TriggerState(_op, LifecycleState.NotStarted, LifecycleState.NotStarted, LifecycleState.Starting,
                ref _state);
            try
            {
                foreach (var type in _programTypes)
                {
                    var info = type.GetCustomAttribute(typeof(ProgramInfoAttribute)) as ProgramInfoAttribute ??
                               throw new ApplicationException(
                                   $"{type.FullName} supplied as program but did not have {nameof(ProgramInfoAttribute)}");
                    var program = Activator.CreateInstance(type) as Program ??
                                  throw new ApplicationException(
                                      $"{type.FullName} supplied as program but could not be casted to {nameof(Program)}");
                    Programs.Add(info.ProgCode, (program, info));
                    if (info.Intrinsic)
                        IntrinsicPrograms.Add(info.Name, (program, info));
                }

                foreach (var type in _serviceTypes)
                {
                    var info = type.GetCustomAttribute(typeof(ServiceInfoAttribute)) as ServiceInfoAttribute ??
                               throw new ApplicationException(
                                   $"{type.FullName} supplied as service but did not have {nameof(ServiceInfoAttribute)}");
                    var service = Activator.CreateInstance(type) as Service ??
                                  throw new ApplicationException(
                                      $"{type.FullName} supplied as service but could not be casted to {nameof(Service)}");
                    Services.Add(info.ProgCode, (service, info));
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
                        world.Tick();
                    }

                    long ms2 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    long ms3;
                    if (lastSave + saveDelayMs < ms2)
                    {
                        Console.WriteLine($"[Database saving {DateTime.Now}]");
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
                    Console.WriteLine(e);
                    foreach (var x in e.Entries)
                    {
                        Console.WriteLine($"{x.Entity}");
                        switch (x.Entity)
                        {
                            case FileModel y:
                                Console.WriteLine($"[{y.Path}] [{y.Name}]");
                                Console.WriteLine("Current:");
                                foreach (var z in x.CurrentValues.Properties)
                                {
                                    Console.WriteLine(
                                        $"{z.Name} // {z} // [{x.CurrentValues[z]}] vs [{x.OriginalValues[z]}]");
                                }

                                break;
                        }
                    }

                    throw;
                }
                finally
                {
                    DecrementCountdown();
                }
            }
        }

        public void QueueConnectCommand(HostConnection context, UserModel user, Guid operationId, int conWidth)
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

                var person = context.GetPerson(world);

                _inputQueue.Enqueue(ServerUtil.InitTentativeProgramContext(world, operationId, context, person,
                    Array.Empty<string>(),
                    invocationType: person.StartedUp
                        ? ProgramContext.InvocationType.Connect
                        : ProgramContext.InvocationType.StartUp, conWidth: conWidth));
            }
            finally
            {
                _queueOp.Set();
            }
        }

        public void QueueCommand(HostConnection context, UserModel user, Guid operationId, int conWidth, string[] line)
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
                    context.GetPerson(world), line, conWidth: conWidth));
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
                Console.WriteLine($"Disconnecting {id}");
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
            Console.WriteLine("Committing database...");
            try
            {
                await Database.SyncAsync();
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
                    connection.WriteEventSafe(new ServerDisconnectEvent {Reason = "Server shutting down."});
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

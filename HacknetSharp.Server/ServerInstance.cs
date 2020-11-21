using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server
{
    public class ServerInstance : Common.Server
    {
        private readonly HashSet<Type> _programTypes;
        private readonly Dictionary<Guid, Program> _programs;
        private readonly CountdownEvent _countdown;
        private readonly AutoResetEvent _op;
        private readonly ConcurrentDictionary<Guid, Connection> _connections;
        private LifecycleState _state;

        private readonly TcpListener _connectListener;
        private Task? _connectTask;
        internal X509Certificate Cert { get; }
        internal AccessController AccessController { get; }
        public Dictionary<Guid, WorldInstance> Worlds { get; }

        protected internal ServerInstance(ServerConfig config)
        {
            var accessControllerType = config.AccessControllerType ??
                                       throw new ArgumentException(
                                           $"{nameof(ServerConfig.AccessControllerType)} not specified");
            var storageContextFactoryType = config.StorageContextFactoryType ??
                                            throw new ArgumentException(
                                                $"{nameof(ServerConfig.StorageContextFactoryType)} not specified");
            Cert = config.Certificate ?? throw new ArgumentException(
                $"{nameof(ServerConfig.Certificate)} not specified");
            AccessController = (AccessController)(Activator.CreateInstance(accessControllerType) ??
                                                  throw new ApplicationException());
            AccessController.Server = this;
            var factory = (StorageContextFactoryBase)(Activator.CreateInstance(storageContextFactoryType) ??
                                                      throw new ApplicationException());
            var context = factory.CreateDbContext(Array.Empty<string>());
            Database = new ServerDatabaseInstance(context);
            Worlds = new Dictionary<Guid, WorldInstance>();
            // TODO inject worlds
            _programTypes = config.Programs;
            _programs = new Dictionary<Guid, Program>();
            _countdown = new CountdownEvent(1);
            _op = new AutoResetEvent(true);
            _connectListener = new TcpListener(IPAddress.Any, config.Port);
            _connections = new ConcurrentDictionary<Guid, Connection>();
            _state = LifecycleState.NotStarted;
        }


        private void RunConnectListener()
        {
            _connectListener.Start();
            _connectTask = Task.Run(async () =>
            {
                while (TryIncrementCountdown(LifecycleState.Active, LifecycleState.Active))
                {
                    try
                    {
                        var connection = new Connection(this, await _connectListener.AcceptTcpClientAsync());
                        _connections.TryAdd(connection.Id, connection);
                    }
                    finally
                    {
                        Util.DecrementCountdown(_op, _countdown);
                    }
                }
            });
        }

        public Task<Task> StartAsync()
        {
            Util.TriggerState(_op, LifecycleState.NotStarted, LifecycleState.NotStarted, LifecycleState.Starting,
                ref _state);
            try
            {
                foreach (var type in _programTypes)
                {
                    var program = Activator.CreateInstance(type) as Program ??
                                  throw new ApplicationException(
                                      $"{type.FullName} supplied as program but could not be casted to {nameof(Program)}");
                    _programs.Add(program.Id, program);
                }
            }
            catch
            {
                Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Failed,
                    ref _state);
                throw;
            }

            Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Active, ref _state);
            RunConnectListener();
            return Task.FromResult(UpdateAsync());
        }

        private async Task UpdateAsync()
        {
            while (TryIncrementCountdown(LifecycleState.Active, LifecycleState.Active))
            {
                try
                {
                    // TODO get queued inputs from connections
                    foreach (var world in Worlds.Values)
                    {
                        world.Tick();
                        Database.AddBulk(world.RegistrationSet);
                        Database.EditBulk(world.DirtySet);
                        Database.DeleteBulk(world.DeregistrationSet);
                        await Database.SyncAsync();
                    }

                    await Task.Delay(10).Caf();
                }
                finally
                {
                    DecrementCountdown();
                }
            }
        }

        public async Task DisposeAsync()
        {
            Util.RequireState(_state, LifecycleState.Starting, LifecycleState.Active);
            while (_state != LifecycleState.Active) await Task.Delay(100).Caf();
            Util.TriggerState(_op, LifecycleState.Active, LifecycleState.Active, LifecycleState.Dispose, ref _state);
            _connectListener.Stop();
            var connectionIds = _connections.Keys;
            foreach (var id in connectionIds)
                DisconnectConnection(id);
            await Task.Run(() =>
            {
                _op.WaitOne();
                _countdown.Signal();
                _op.Set();
                _countdown.Wait();
            });
            Util.TriggerState(_op, LifecycleState.Dispose, LifecycleState.Dispose, LifecycleState.Disposed, ref _state);
        }

        internal void SelfRemoveConnection(Guid id) => _connections.TryRemove(id, out _);

        internal void DisconnectConnection(Guid id)
        {
            if (_connections.TryRemove(id, out var connection))
                connection.CancellationTokenSource.Cancel();
        }

        internal bool TryIncrementCountdown(LifecycleState min, LifecycleState max) =>
            Util.TryIncrementCountdown(_op, _countdown, _state, min, max);

        internal void DecrementCountdown() => Util.DecrementCountdown(_op, _countdown);
    }
}

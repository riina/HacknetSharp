﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server
{
    public class Server
    {
        private readonly HashSet<Type> _programTypes;
        private readonly Dictionary<string, (Program, ProgramInfoAttribute)> _programs;
        private readonly CountdownEvent _countdown;
        private readonly AutoResetEvent _op;
        private readonly ConcurrentDictionary<Guid, HostConnection> _connections;
        private LifecycleState _state;

        private readonly TcpListener _connectListener;
        private Task? _connectTask;
        internal X509Certificate Cert { get; }
        internal AccessController AccessController { get; }
        public Dictionary<Guid, World> Worlds { get; }
        public ServerDatabase Database { get; protected set; }

        protected internal Server(ServerConfig config)
        {
            var accessControllerType = config.AccessControllerType ??
                                       throw new ArgumentException(
                                           $"{nameof(ServerConfig.AccessControllerType)} not specified");
            var storageContextFactoryType = config.StorageContextFactoryType ??
                                            throw new ArgumentException(
                                                $"{nameof(ServerConfig.StorageContextFactoryType)} not specified");
            Cert = config.Certificate ?? throw new ArgumentException(
                $"{nameof(ServerConfig.Certificate)} not specified");
            var factory = (StorageContextFactoryBase)(Activator.CreateInstance(storageContextFactoryType) ??
                                                      throw new ApplicationException());
            var context = factory.CreateDbContext(Array.Empty<string>());
            Database = new ServerDatabase(context);
            AccessController = new AccessController(this);
            Worlds = new Dictionary<Guid, World>();
            // TODO inject worlds
            _programTypes = new HashSet<Type>(ServerUtil.DefaultPrograms);
            _programTypes.UnionWith(config.Programs);
            _programs = new Dictionary<string, (Program, ProgramInfoAttribute)>();
            _countdown = new CountdownEvent(1);
            _op = new AutoResetEvent(true);
            _connectListener = new TcpListener(IPAddress.Any, config.Port);
            _connections = new ConcurrentDictionary<Guid, HostConnection>();
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
                        var connection = new HostConnection(this, await _connectListener.AcceptTcpClientAsync().Caf());
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
                    var info = type.GetCustomAttribute(typeof(ProgramInfoAttribute)) as ProgramInfoAttribute ??
                               throw new ApplicationException(
                                   $"{type.FullName} supplied as program but did not have {nameof(ProgramInfoAttribute)}");
                    var program = Activator.CreateInstance(type) as Program ??
                                  throw new ApplicationException(
                                      $"{type.FullName} supplied as program but could not be casted to {nameof(Program)}");
                    _programs.Add(info.Name, (program, info));
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
                        await Database.SyncAsync().Caf();
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
            }).Caf();
            Util.TriggerState(_op, LifecycleState.Dispose, LifecycleState.Dispose, LifecycleState.Disposed, ref _state);
        }

        internal void SelfRemoveConnection(Guid id) => _connections.TryRemove(id, out _);

        internal void DisconnectConnection(Guid id)
        {
            if (_connections.TryRemove(id, out var connection))
                connection.Dispose();
        }

        internal bool TryIncrementCountdown(LifecycleState min, LifecycleState max) =>
            Util.TryIncrementCountdown(_op, _countdown, _state, min, max);

        internal void DecrementCountdown() => Util.DecrementCountdown(_op, _countdown);
    }
}
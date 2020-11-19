using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Ns;

namespace HacknetSharp.Server
{
    public class ServerInstance
    {
        private readonly AccessController _accessController;
        private readonly WorldDatabase _worldDatabase;
        private readonly HashSet<Type> _programTypes;
        private readonly CountdownEvent _countdown;
        private readonly AutoResetEvent _op;
        private State _state;


        private readonly TcpListener _connectListener;
        private readonly X509Certificate _cert;
        private Task? _connectTask;

        protected internal ServerInstance(ServerConfig config)
        {
            var accessControllerType = config.AccessControllerType ??
                                       throw new ArgumentException(
                                           $"{nameof(ServerConfig.AccessControllerType)} not specified");
            var storageContextFactoryType = config.StorageContextFactoryType ??
                                            throw new ArgumentException(
                                                $"{nameof(ServerConfig.StorageContextFactoryType)} not specified");
            _accessController = (AccessController)(Activator.CreateInstance(accessControllerType) ??
                                                   throw new ApplicationException());
            var factory = (StorageContextFactoryBase)(Activator.CreateInstance(storageContextFactoryType) ??
                                                      throw new ApplicationException());
            var context = factory.CreateDbContext(Array.Empty<string>());
            _worldDatabase = new WorldDatabase(context);
            _programTypes = config.Programs;
            _countdown = new CountdownEvent(1);
            _op = new AutoResetEvent(true);
            _connectListener = new TcpListener(IPAddress.Any, config.Port);
            _state = State.NotStarted;
        }


        private void RunConnectListener()
        {
            _connectListener.Start();
            _connectTask = Task.Run(async () =>
            {
                while (TryIncrementCountdown(State.Active, State.Active))
                {
                    try
                    {
                        await _connectListener.AcceptTcpClientAsync().ContinueWith(HandleAsyncConnection);
                    }
                    finally
                    {
                        DecrementCountdown();
                    }
                }
            });
        }

        private async Task HandleAsyncConnection(Task<TcpClient> task)
        {
            if (!TryIncrementCountdown(State.Active, State.Active)) return;
            try
            {
                var client = await task;
                await using var sslStream = new SslStream(client.GetStream(), false, default, default,
                    EncryptionPolicy.RequireEncryption);
                // Authenticate the server but don't require the client to authenticate
                await sslStream.AuthenticateAsServerAsync(_cert, false, true);
                sslStream.ReadTimeout = 10 * 1000;
                sslStream.WriteTimeout = 10 * 1000;
                var ns = new NetSerializer(sslStream);
                string? text = ns.ReadUtf8String();
                Console.WriteLine(text);
                // TODO handle user
            }
            finally
            {
                DecrementCountdown();
            }
        }

        public async Task<Task> StartAsync()
        {
            TriggerState(State.NotStarted, State.NotStarted, State.Starting);
            // TODO initialize programs
            TriggerState(State.Starting, State.Starting, State.Active);
            RunConnectListener();
            return UpdateAsync();
        }

        private async Task UpdateAsync()
        {
            while (TryIncrementCountdown(State.Active, State.Active))
            {
                try
                {
                    // TODO get queued inputs from connections
                    // TODO update worlds lockstep

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
            RequireState(State.Starting, State.Active);
            while (_state != State.Active) await Task.Delay(100).Caf();
            TriggerState(State.Active, State.Active, State.Dispose);
            await Task.Run(() =>
            {
                _op.WaitOne();
                _countdown.Signal();
                _op.Set();
                _countdown.Wait();
            });
            _connectListener.Stop();
            TriggerState(State.Dispose, State.Dispose, State.Disposed);
        }

        private void TriggerState(State min, State max, State target)
        {
            _op.WaitOne();
            try
            {
                RequireState(min, max);
                _state = target;
            }
            finally
            {
                _op.Set();
            }
        }

        private void RequireState(State min, State max)
        {
            if ((int)_state < (int)min)
                throw new InvalidOperationException(
                    $"Cannot perform this action that requires state {min} when object is in state {_state}");
            if ((int)_state > (int)max)
                throw new InvalidOperationException(
                    $"Cannot perform this action that requires state {max} when object is in state {_state}");
        }

        private bool TryIncrementCountdown(State min, State max)
        {
            _op.WaitOne();
            bool keepGoing = !((int)_state < (int)min || (int)_state > (int)max);
            if (keepGoing)
                _countdown.AddCount();
            _op.Set();
            return keepGoing;
        }

        private void DecrementCountdown()
        {
            _op.WaitOne();
            _countdown.Signal();
            _op.Set();
        }

        private enum State
        {
            NotStarted,
            Starting,
            Active,
            Dispose,
            Disposed
        }
    }
}

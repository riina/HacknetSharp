using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Common;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server
{
    public class HostConnection : IHostConnection
    {
        public Guid Id { get; }
        public LifecycleState State { get; private set; }
        public Task ExecutionTask { get; }
        private readonly Server _server;
        private readonly TcpClient _client;
        private readonly AutoResetEvent _lockOutOp;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private SslStream? _sslStream;
        private BufferedStream? _bufferedStream;
        private bool _closed;
        private bool _connected;

        public HostConnection(Server server, TcpClient client)
        {
            Id = Guid.NewGuid();
            State = LifecycleState.Starting;
            _server = server;
            _client = client;
            _lockOutOp = new AutoResetEvent(true);
            _cancellationTokenSource = new CancellationTokenSource();
            PlayerModels = new Dictionary<Guid, PlayerModel>();
            ExecutionTask = Execute(_cancellationTokenSource.Token);
        }

        private async Task Execute(CancellationToken cancellationToken)
        {
            if (!_server.TryIncrementCountdown(LifecycleState.Active, LifecycleState.Active)) return;
            try
            {
                _sslStream = new SslStream(_client.GetStream(), false, default, default,
                    EncryptionPolicy.RequireEncryption);
                // Try and kill the stream, accept the consequences
                cancellationToken.Register(Dispose);
                // Authenticate the server but don't require the client to authenticate
                SslServerAuthenticationOptions opts =
                    new SslServerAuthenticationOptions
                    {
                        ServerCertificate = _server.Cert,
                        ClientCertificateRequired = false,
                        CertificateRevocationCheckMode = X509RevocationMode.Online
                    };
                await _sslStream.AuthenticateAsServerAsync(opts, cancellationToken);
                _connected = true;
                _sslStream.ReadTimeout = 10 * 1000;
                _sslStream.WriteTimeout = 10 * 1000;
                _bufferedStream = new BufferedStream(_sslStream);
                ClientEvent? evt;
                UserModel? user = null;
                while (!((evt = await _bufferedStream.ReadEventAsync<ClientEvent>(cancellationToken)) is
                    ClientDisconnectEvent))
                {
                    // TODO handle forgeregtoken / login + regtoken
                    switch (evt)
                    {
                        case LoginEvent login:
                        {
                            if (user != null)
                            {
                                _bufferedStream.WriteEvent(LoginFailEvent.Singleton);
                                break;
                            }

                            user = await _server.AccessController.AuthenticateAsync(login.User, login.Pass);
                            if (user == null)
                            {
                                _bufferedStream.WriteEvent(LoginFailEvent.Singleton);
                                _bufferedStream.WriteEvent(ServerDisconnectEvent.Singleton);
                                await _bufferedStream.FlushAsync(cancellationToken);
                                return;
                            }

                            _sslStream.ReadTimeout = 100 * 1000;
                            _sslStream.WriteTimeout = 100 * 1000;

                            // TODO check or generate / register player model

                            // TODO provide basic user state
                            _bufferedStream.WriteEvent(new UserInfoEvent());
                            break;
                        }
                        case CommandEvent command:
                        {
                            if (user == null) continue;
                            // TODO operate on command based on context, this is temporary
                            _bufferedStream.WriteEvent(new OutputEvent {Text = "Output is not yet implemented."});
                            break;
                        }
                    }

                    await _bufferedStream.FlushAsync(cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                    _server.SelfRemoveConnection(Id);
                _server.DecrementCountdown();
                Dispose();
            }
        }

        public void Dispose()
        {
            if (_closed) return;

            _connected = false;
            _closed = true;
            _cancellationTokenSource.Cancel();

            try
            {
                _sslStream?.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                _client.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public Dictionary<Guid, PlayerModel> PlayerModels { get; }

        public void WriteEvent(ServerEvent evt)
        {
            if (_closed || _bufferedStream == null) throw new InvalidOperationException();
            _lockOutOp.WaitOne();
            try
            {
                _bufferedStream.WriteEvent(evt);
            }
            finally
            {
                _lockOutOp.Set();
            }
        }

        public void WriteEvents(IEnumerable<ServerEvent> events)
        {
            if (_closed || _bufferedStream == null) throw new InvalidOperationException();
            _lockOutOp.WaitOne();
            try
            {
                foreach (var evt in events) _bufferedStream.WriteEvent(evt);
            }
            finally
            {
                _lockOutOp.Set();
            }
        }

        public Task FlushAsync() => FlushAsync(CancellationToken.None);

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_closed || _bufferedStream == null) throw new InvalidOperationException();
            {
                _lockOutOp.WaitOne();
                await _bufferedStream.FlushAsync(cancellationToken);
                _lockOutOp.Set();
            }
        }

        public bool Connected => _connected;
    }
}

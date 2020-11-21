using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;

namespace HacknetSharp.Server
{
    public class Connection
    {
        public Guid Id { get; }
        public LifecycleState State { get; private set; }
        public CancellationTokenSource CancellationTokenSource { get; }
        public Task ExecutionTask { get; }
        private readonly ServerInstance _server;
        private readonly TcpClient _client;
        private SslStream? _stream;
        private bool _closed;

        public Connection(ServerInstance server, TcpClient client)
        {
            Id = Guid.NewGuid();
            State = LifecycleState.Starting;
            _server = server;
            _client = client;
            CancellationTokenSource = new CancellationTokenSource();
            ExecutionTask = Task.Run(async () => await Execute(CancellationTokenSource.Token));
        }

        private async Task Execute(CancellationToken cancellationToken)
        {
            if (!_server.TryIncrementCountdown(LifecycleState.Active, LifecycleState.Active)) return;
            try
            {
                _stream = new SslStream(_client.GetStream(), false, default, default,
                    EncryptionPolicy.RequireEncryption);
                // Try and kill the stream, accept the consequences
                cancellationToken.Register(CloseStream);
                // Authenticate the server but don't require the client to authenticate
                SslServerAuthenticationOptions opts =
                    new SslServerAuthenticationOptions
                    {
                        ServerCertificate = _server.Cert,
                        ClientCertificateRequired = false,
                        CertificateRevocationCheckMode = X509RevocationMode.Online
                    };
                await _stream.AuthenticateAsServerAsync(opts, cancellationToken);
                _stream.ReadTimeout = 10 * 1000;
                _stream.WriteTimeout = 10 * 1000;
                var bs = new BufferedStream(_stream);
                ClientEvent evt;
                while (!((evt = bs.ReadEvent<ClientEvent>()) is ClientDisconnectEvent))
                {
                    switch (evt)
                    {
                        case LoginEvent loginEvent:
                        {
                            if (!await _server.AccessController.AuthenticateAsync(loginEvent.User, loginEvent.Pass))
                            {
                                bs.WriteEvent(LoginFailEvent.Singleton);
                                return;
                            }

                            // TODO provide basic user state
                            break;
                        }
                    }

                    await bs.FlushAsync(cancellationToken);
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                if (!CancellationTokenSource.IsCancellationRequested)
                    _server.SelfRemoveConnection(Id);
                _server.DecrementCountdown();
                CloseStream();
            }
        }

        private void CloseStream()
        {
            lock (_client)
                if (!_closed)
                {
                    _stream?.Close();
                    _closed = true;
                }
        }
    }
}

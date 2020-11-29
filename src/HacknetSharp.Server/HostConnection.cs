using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    public class HostConnection : IPersonContext
    {
        public Guid Id { get; }

        public UserModel? User { get; private set; }
        private readonly Server _server;
        private readonly TcpClient _client;
        private readonly AutoResetEvent _lockOutOp;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private PlayerModel? _playerModel;
        private SslStream? _sslStream;
        private BufferedStream? _bufferedStream;
        private bool _closed;
        private bool _connected;

        public HostConnection(Server server, TcpClient client)
        {
            Id = Guid.NewGuid();
            _server = server;
            _client = client;
            _lockOutOp = new AutoResetEvent(true);
            _cancellationTokenSource = new CancellationTokenSource();
            _ = Execute(_cancellationTokenSource.Token);
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
                await _sslStream.AuthenticateAsServerAsync(opts, cancellationToken).Caf();
                _connected = true;
                _sslStream.ReadTimeout = 10 * 1000;
                _sslStream.WriteTimeout = 10 * 1000;
                _bufferedStream = new BufferedStream(_sslStream);
                ClientEvent? evt;
                User = null;
                while (!((evt = await _sslStream.ReadEventAsync<ClientEvent>(cancellationToken).Caf()) is
                    ClientDisconnectEvent))
                {
                    if (evt == null) return;
                    switch (evt)
                    {
                        case LoginEvent login:
                        {
                            var op = login.Operation;
                            if (User != null)
                            {
                                WriteEvent(new LoginFailEvent {Operation = op});
                                break;
                            }

                            if (login.RegistrationToken != null)
                                User = await _server.AccessController
                                    .RegisterAsync(login.User, login.Pass, login.RegistrationToken).Caf();
                            else
                                User = await _server.AccessController.AuthenticateAsync(login.User, login.Pass).Caf();
                            if (User == null)
                            {
                                WriteEvent(new LoginFailEvent {Operation = op});
                                WriteEvent(new ServerDisconnectEvent {Reason = "Invalid login."});
                                await FlushAsync(cancellationToken).Caf();
                                return;
                            }

                            _sslStream.ReadTimeout = 100 * 1000;
                            _sslStream.WriteTimeout = 100 * 1000;

                            WriteEvent(new UserInfoEvent {Operation = op, Admin = User.Admin});
                            WriteEvent(new OutputEvent {Text = "Welcome to ossu. Type \"exit\" to exit."});
                            break;
                        }
                        case RegistrationTokenForgeRequestEvent forgeRequest:
                        {
                            var op = forgeRequest.Operation;
                            var random = new Random();
                            var arr = new byte[32];
                            if (User == null) continue;
                            if (!User.Admin)
                            {
                                WriteEvent(new AccessFailEvent {Operation = op});
                                break;
                            }

                            string token;
                            do
                            {
                                random.NextBytes(arr);
                                token = Convert.ToBase64String(arr);
                            } while (await _server.Database.GetAsync<string, RegistrationToken>(token).Caf() != null);

                            var tokenModel = new RegistrationToken {Forger = User, Key = token};
                            _server.Database.Add(tokenModel);
                            await _server.Database.SyncAsync().Caf();
                            WriteEvent(new RegistrationTokenForgeResponseEvent(op, token));
                            break;
                        }
                        case CommandEvent command:
                        {
                            var op = command.Operation;
                            if (User == null) continue;
                            var line = Arguments.SplitCommandLine(command.Text);
                            if (line.Length > 0 && line[0].Equals("exit", StringComparison.InvariantCultureIgnoreCase))
                            {
                                WriteEvent(new OperationCompleteEvent {Operation = op});
                                WriteEvent(new ServerDisconnectEvent {Reason = "Shell closed."});
                                await FlushAsync(cancellationToken).Caf();
                                return;
                            }
                            else
                                _server.Execute(this, op, line);

                            break;
                        }
                    }

                    await FlushAsync(cancellationToken).Caf();
                }
            }
            catch (IOException)
            {
                // ignored
            }
            catch (OperationCanceledException)
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

            try
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            catch
            {
                // ignored
            }

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
            foreach (var evt in events) _bufferedStream.WriteEvent(evt);
        }

        public Task FlushAsync() => FlushAsync(CancellationToken.None);

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_closed || _bufferedStream == null) throw new InvalidOperationException();
            _lockOutOp.WaitOne();
            try
            {
                await _bufferedStream.FlushAsync(cancellationToken).Caf();
            }
            finally
            {
                _lockOutOp.Set();
            }
        }

        public PersonModel GetPerson(IWorld world)
        {
            if (User == null) throw new InvalidOperationException();
            var playerModel = GetPlayerModel();
            var wId = world.Model.Key;
            PersonModel? person = playerModel.Identities.FirstOrDefault(x => x.World.Key == wId);
            if (person != null) return person;
            person = CreateAndRegisterNewPersonAndSystem(_server, world, playerModel);
            _server.RegisterModel(person);
            return person;
        }

        private static PersonModel CreateAndRegisterNewPersonAndSystem(Server server, IWorld world,
            PlayerModel player)
        {
            var person = server.Spawn.Person(world.Model, player.Key, player.Key);
            var system = server.Spawn.System(world.Model, world.PlayerSystemTemplate, person, player.User.Hash,
                player.User.Salt);
            person.DefaultSystem = system;
            person.CurrentSystem = system;
            return person;
        }

        public PlayerModel GetPlayerModel()
        {
            if (User == null) throw new InvalidOperationException();

            // Get from connection
            if (_playerModel != null) return _playerModel;

            _playerModel = _server.Database.GetAsync<string, PlayerModel>(User.Key).Result;
            if (_playerModel == null)
            {
                var world = _server.DefaultWorld;

                _playerModel = _server.Spawn.Player(User);
                _server.RegisterModel(_playerModel);

                CreateAndRegisterNewPersonAndSystem(_server, world, _playerModel);
            }

            // TODO additional set to recognize initialization per world, move reset logic

            // Reset to existing world if necessary
            if (!_server.Worlds.TryGetValue(_playerModel.ActiveWorld, out var curWorld))
                _playerModel.ActiveWorld = (curWorld = _server.DefaultWorld).Model.Key;

            var person = GetPerson(curWorld);

            // Reset user state
            person.CurrentSystem = person.DefaultSystem;
            person.WorkingDirectory = "/";

            return _playerModel;
        }

        public void WriteEventSafe(ServerEvent evt)
        {
            try
            {
                WriteEvent(evt);
            }
            catch
            {
                // Ignored
            }
        }

        public async Task FlushSafeAsync()
        {
            await Task.Yield();
            try
            {
                await FlushAsync(CancellationToken.None);
            }
            catch
            {
                // Ignored
            }
        }

        public bool Connected => _connected;
    }
}

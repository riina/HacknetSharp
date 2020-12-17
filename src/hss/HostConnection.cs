using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp;
using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;

namespace hss
{
    public class HostConnection : IPersonContext
    {
        public Guid Id { get; }
        public ConcurrentDictionary<Guid, ClientResponseEvent> Responses { get; }
        public string? UserName => _user?.Key;

        private UserModel? _user;
        private readonly Server _server;
        private readonly TcpClient _client;
        private readonly AutoResetEvent _lockOutOp;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly HashSet<Guid> _initializedWorlds;
        private SslStream? _sslStream;
        private readonly Queue<ServerEvent> _writeEventQueue;
        private readonly ConcurrentQueue<ReadOnlyMemory<byte>> _writeQueue;
        private bool _closed;
        private bool _connected;

        public HostConnection(Server server, TcpClient client)
        {
            Id = Guid.NewGuid();
            _server = server;
            _client = client;
            _lockOutOp = new AutoResetEvent(true);
            Responses = new ConcurrentDictionary<Guid, ClientResponseEvent>();
            _cancellationTokenSource = new CancellationTokenSource();
            _initializedWorlds = new HashSet<Guid>();
            _writeEventQueue = new Queue<ServerEvent>();
            _writeQueue = new ConcurrentQueue<ReadOnlyMemory<byte>>();
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
                    new()
                    {
                        ServerCertificate = _server.Cert,
                        ClientCertificateRequired = false,
                        CertificateRevocationCheckMode = X509RevocationMode.Online
                    };
                await _sslStream.AuthenticateAsServerAsync(opts, cancellationToken).Caf();
                _connected = true;
                _sslStream.ReadTimeout = 10 * 1000;
                _sslStream.WriteTimeout = 10 * 1000;
                ClientEvent? evt;
                _user = null;
                while (!((evt = await _sslStream.ReadEventAsync<ClientEvent>(cancellationToken).Caf()) is
                    ClientDisconnectEvent))
                {
                    if (evt == null) return;
                    switch (evt)
                    {
                        case LoginEvent login:
                        {
                            var op = login.Operation;
                            if (_user != null)
                            {
                                WriteEvent(new LoginFailEvent {Operation = op});
                                break;
                            }

                            if (_server.HasConnection(login.User))
                            {
                                if (login.RegistrationToken != null)
                                {
                                    WriteEvent(new LoginFailEvent {Operation = op});
                                    WriteEvent(new ServerDisconnectEvent {Reason = "Registration failed."});
                                }
                                else
                                {
                                    WriteEvent(new LoginFailEvent {Operation = op});
                                    WriteEvent(new ServerDisconnectEvent {Reason = "Invalid login."});
                                }

                                await FlushAsync(cancellationToken).Caf();
                                return;
                            }

                            if (login.RegistrationToken != null)
                            {
                                _user = await _server.AccessController
                                    .RegisterAsync(login.User, login.Pass, login.RegistrationToken).Caf();
                                if (_user != null)
                                    RegisterNewPerson(_server.DefaultWorld, _user);
                                else
                                {
                                    WriteEvent(new LoginFailEvent {Operation = op});
                                    WriteEvent(new ServerDisconnectEvent {Reason = "Registration failed."});
                                    await FlushAsync(cancellationToken).Caf();
                                    return;
                                }
                            }
                            else
                            {
                                _user = await _server.AccessController.AuthenticateAsync(login.User, login.Pass).Caf();
                                if (_user == null)
                                {
                                    WriteEvent(new LoginFailEvent {Operation = op});
                                    WriteEvent(new ServerDisconnectEvent {Reason = "Invalid login."});
                                    await FlushAsync(cancellationToken).Caf();
                                    return;
                                }
                            }

                            // Reset to existing world if necessary
                            if (!_server.Worlds.ContainsKey(_user.ActiveWorld))
                            {
                                _user.ActiveWorld = _server.DefaultWorld.Model.Key;
                                var w3 = _server.DefaultWorld.Model.Key;
                                if (_user.Identities.All(p => p.World.Key != w3))
                                    RegisterNewPerson(_server.DefaultWorld, _user);
                            }

                            _sslStream.ReadTimeout = 100 * 1000;
                            _sslStream.WriteTimeout = 100 * 1000;

                            WriteEvent(new UserInfoEvent {Operation = op, Admin = _user.Admin});
                            WriteEvent(new OutputEvent {Text = "<< LOGGED IN >>\n"});
                            break;
                        }
                        case RegistrationTokenForgeRequestEvent forgeRequest:
                        {
                            var op = forgeRequest.Operation;
                            var random = new Random();
                            var arr = new byte[32];
                            if (_user == null) continue;
                            if (!_user.Admin)
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

                            var tokenModel = new RegistrationToken {Forger = _user, Key = token};
                            _server.Database.Add(tokenModel);
                            await _server.Database.SyncAsync().Caf();
                            WriteEvent(new RegistrationTokenForgeResponseEvent(op, token));
                            break;
                        }
                        case InitialCommandEvent command:
                        {
                            var op = command.Operation;
                            if (_user == null)
                            {
                                WriteEvent(new OperationCompleteEvent {Operation = op});
                                break;
                            }

                            _server.QueueConnectCommand(this, _user, op, command.ConWidth);
                            break;
                        }
                        case CommandEvent command:
                        {
                            var op = command.Operation;
                            if (_user == null)
                            {
                                WriteEvent(new OperationCompleteEvent {Operation = op});
                                break;
                            }

                            _server.QueueCommand(this, _user, op, command.ConWidth,
                                ServerUtil.SplitCommandLine(command.Text));

                            break;
                        }
                        case ClientResponseEvent response:
                        {
                            Responses.AddOrUpdate(response.Operation, response, (id, e) => e);
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
            if (_closed || _sslStream == null) throw new InvalidOperationException();
            lock (_writeEventQueue)
                _writeEventQueue.Enqueue(evt);
        }

        public void WriteEvents(IEnumerable<ServerEvent> events)
        {
            if (_closed || _sslStream == null) throw new InvalidOperationException();
            lock (_writeEventQueue)
                foreach (var evt in events)
                    _writeEventQueue.Enqueue(evt);
        }

        public Task FlushAsync() => FlushAsync(CancellationToken.None);

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_closed || _sslStream == null) throw new InvalidOperationException();
            await Task.Yield();

            var ms = new MemoryStream();
            lock (_writeEventQueue)
            {
                while (_writeEventQueue.TryDequeue(out var evt))
                    ms.WriteEvent(evt);
                ms.TryGetBuffer(out ArraySegment<byte> buf);
                _writeQueue.Enqueue(buf);
            }

            _lockOutOp.WaitOne();
            try
            {
                while (_writeQueue.TryDequeue(out var buf2))
                {
                    await _sslStream.WriteAsync(buf2, cancellationToken).Caf();
                    await _sslStream.FlushAsync(cancellationToken).Caf();
                }
            }
            finally
            {
                _lockOutOp.Set();
            }
        }

        public PersonModel GetPerson(IWorld world)
        {
            if (_user == null) throw new InvalidOperationException();
            var wId = world.Model.Key;
            PersonModel person = _user.Identities.FirstOrDefault(x => x.World.Key == wId)
                                 ?? RegisterNewPerson(world, _user);

            if (_initializedWorlds.Add(wId))
            {
                // Reset user state
                var systemModelKey = person.DefaultSystem;
                var system = world.Model.Systems.FirstOrDefault(x => x.Key == systemModelKey)
                             ?? RegisterNewSystem(world, _user, person);
                var pk = person.Key;
                var login = system.Logins.FirstOrDefault(l => l.Person == pk)
                            ?? world.Spawn.Login(system, person.UserName,
                                _user.Hash, _user.Salt, true, person);
                world.StartShell(this, person, system, login, ServerConstants.ShellName);
            }

            return person;
        }

        private static PersonModel RegisterNewPerson(IWorld world, UserModel user)
        {
            var person = world.Spawn.Person(user.Key, user.Key, user);
            RegisterNewSystem(world, user, person);
            return person;
        }

        private static SystemModel RegisterNewSystem(IWorld world, UserModel user,
            PersonModel person)
        {
            var system = world.Spawn.System(world.PlayerSystemTemplate, person, user.Hash, user.Salt,
                new IPAddressRange(world.Model.PlayerAddressRange));
            person.DefaultSystem = system.Key;
            return system;
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

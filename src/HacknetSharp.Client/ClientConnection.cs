using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;

namespace HacknetSharp.Client
{
    public class ClientConnection : IInboundConnection<ServerEvent>, IOutboundConnection<ClientEvent>
    {
        public string Server { get; }
        public ushort Port { get; }
        public string User { get; }
        public Action<ServerEvent> OnReceivedEvent { get; set; } = null!;
        public Action<ServerDisconnectEvent> OnDisconnect { get; set; } = null!;
        private string? _pass;
        private string? _registrationToken;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly AutoResetEvent _op;
        private readonly AutoResetEvent _lockInOp;
        private readonly AutoResetEvent _lockOutOp;
        private readonly List<ServerEvent> _inEvents;
        private Task? _inTask;
        private LifecycleState _state;
        private bool _connected;
        private bool _closed;
        private TcpClient? _client;
        private SslStream? _sslStream;
        private readonly ConcurrentQueue<ClientEvent> _writeEventQueue;
        private readonly ConcurrentQueue<ArraySegment<byte>> _writeQueue;

        public ClientConnection(string server, ushort port, string user, string pass, string? registrationToken = null)
        {
            Server = server;
            Port = port;
            User = user;
            _cancellationTokenSource = new CancellationTokenSource();
            _pass = pass;
            _registrationToken = registrationToken;
            _op = new AutoResetEvent(true);
            _lockInOp = new AutoResetEvent(true);
            _lockOutOp = new AutoResetEvent(true);
            _state = LifecycleState.NotStarted;
            _inEvents = new List<ServerEvent>();
            _writeEventQueue = new ConcurrentQueue<ClientEvent>();
            _writeQueue = new ConcurrentQueue<ArraySegment<byte>>();
        }

        public async Task<UserInfoEvent> ConnectAsync()
        {
            Util.TriggerState(_op, LifecycleState.NotStarted, LifecycleState.NotStarted, LifecycleState.Starting,
                ref _state);
            try
            {
                _client = new TcpClient(Server, Port);
                _sslStream = new SslStream(
                    _client.GetStream(), false, ValidateServerCertificate
                );
                await _sslStream.AuthenticateAsClientAsync(Server, default, SslProtocols.Tls12, true).Caf();
                _inTask = ExecuteReceive(_sslStream, _cancellationTokenSource.Token);
            }
            catch
            {
                Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Failed,
                    ref _state);
                throw;
            }

            try
            {
                var loginOp = Guid.NewGuid();
                var loginCommand = new LoginEvent
                {
                    Operation = loginOp, User = User, Pass = _pass!, RegistrationToken = _registrationToken
                };
                WriteEvent(loginCommand);
                await FlushAsync(_cancellationTokenSource.Token).Caf();
                _pass = null;
                _registrationToken = null;
                var result = await WaitForAsync(e => e is IOperation op && op.Operation == loginOp, 10,
                    _cancellationTokenSource.Token).Caf();
                var info = result switch
                {
                    UserInfoEvent i => i,
                    LoginFailEvent _ => throw new LoginException("Login failed."),
                    _ => throw new ProtocolException($"Unexpected event type {result?.GetType().FullName} received.")
                };

                OnReceivedEvent += e =>
                {
                    if (e is ServerDisconnectEvent ex) OnDisconnect(ex);
                };
                Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Active,
                    ref _state);
                return info;
            }
            catch
            {
                _connected = false;
                try
                {
                    WriteEvent(ClientDisconnectEvent.Singleton);
                    await FlushAsync(_cancellationTokenSource.Token).Caf();
                }
                catch
                {
                    // ignored
                }

                Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Failed,
                    ref _state);
                Dispose();
                throw;
            }
        }

        private async Task ExecuteReceive(SslStream stream, CancellationToken cancellationToken)
        {
            _connected = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                var evt = await stream.ReadEventAsync<ServerEvent>(cancellationToken).Caf();
                if (evt == null) return;
                OnReceivedEvent(evt);
                _lockInOp.WaitOne();
                _inEvents.Add(evt);
                _lockInOp.Set();
            }

            throw new TaskCanceledException();
        }

        // No throws
        public async Task DisposeAsync()
        {
            switch (_state)
            {
                case LifecycleState.NotStarted:
                    _state = LifecycleState.Disposed;
                    return;
                case LifecycleState.Dispose:
                case LifecycleState.Disposed:
                case LifecycleState.Failed:
                    return;
            }

            _connected = false;

            try
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
            catch
            {
                // ignored
            }

            while (_state == LifecycleState.Starting) await Task.Delay(100).Caf();

            try
            {
                Dispose();
            }
            catch
            {
                // ignored
            }

            Util.TriggerState(_op, LifecycleState.Active, LifecycleState.Active, LifecycleState.Disposed, ref _state);
        }

        private static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        private void Dispose()
        {
            if (_closed) return;

            _connected = false;
            _closed = true;

            try
            {
                _sslStream?.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public Task<ServerEvent?> WaitForAsync(Func<ServerEvent, bool> predicate, int pollMillis) =>
            WaitForAsync(predicate, pollMillis, CancellationToken.None);

        public async Task<ServerEvent?> WaitForAsync(Func<ServerEvent, bool> predicate, int pollMillis,
            CancellationToken cancellationToken)
        {
            if (_closed || _inTask == null) throw new InvalidOperationException();
            while (!cancellationToken.IsCancellationRequested)
            {
                _lockInOp.WaitOne();
                var evt = _inEvents.FirstOrDefault(predicate);
                if (evt != null) _inEvents.Remove(evt);
                _lockInOp.Set();
                if (evt != null) return evt;
                if (_inTask.IsFaulted)
                    throw new Exception($"Could not read event: task excepted. Information:\n{_inTask.Exception}");
                if (_inTask.IsCompleted) return null;
                await Task.Delay(pollMillis, cancellationToken).Caf();
            }

            throw new TaskCanceledException();
        }

        public IEnumerable<ServerEvent> ReadEvents()
        {
            _lockInOp.WaitOne();
            var list = new List<ServerEvent>(_inEvents);
            _inEvents.Clear();
            _lockInOp.Set();
            return list;
        }

        public void WriteEvent(ClientEvent evt)
        {
            if (_closed || _sslStream == null) throw new InvalidOperationException();
            _writeEventQueue.Enqueue(evt);
        }

        public void WriteEvents(IEnumerable<ClientEvent> events)
        {
            if (_closed || _sslStream == null) throw new InvalidOperationException();
            foreach (var evt in events) _writeEventQueue.Enqueue(evt);
        }

        public Task FlushAsync() => FlushAsync(CancellationToken.None);

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_closed || _sslStream == null) throw new InvalidOperationException();
            await Task.Yield();

            var ms = new MemoryStream();
            while (_writeEventQueue.TryDequeue(out var evt)) ms.WriteEvent(evt);
            Debug.Assert(ms.TryGetBuffer(out ArraySegment<byte> buf));

            _writeQueue.Enqueue(buf);

            _lockOutOp.WaitOne();
            try
            {
                while (_writeQueue.TryDequeue(out var segment))
                {
                    await _sslStream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken).Caf();
                    await _sslStream.FlushAsync(cancellationToken).Caf();
                }
            }
            finally
            {
                _lockOutOp.Set();
            }
        }

        public bool Connected => _connected;
    }
}

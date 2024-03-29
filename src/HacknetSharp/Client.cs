﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HacknetSharp
{
    /// <summary>
    /// Represents a <see cref="HacknetSharp"/> protocol client.
    /// </summary>
    public class Client : IInboundConnection<ServerEvent>, IOutboundConnection<ClientEvent>
    {
        /// <summary>
        /// Server this client was instantiated with.
        /// </summary>
        public string Server { get; }

        /// <summary>
        /// Port this client was instantiated with.
        /// </summary>
        public ushort Port { get; }

        /// <summary>
        /// Username this client was instantiated with.
        /// </summary>
        public string User { get; }

        /// <summary>
        /// Delegate for event responses.
        /// </summary>
        public Action<ServerEvent> OnReceivedEvent { get; set; }

        /// <summary>
        /// Delegate for disconnect event responses.
        /// </summary>
        public Action<ServerDisconnectEvent> OnDisconnect { get; set; }

        private string? _pass;
        private string? _registrationToken;
        private readonly ILogger _logger;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly AutoResetEvent _op;
        private readonly AutoResetEvent _lockInOp;
        private readonly AutoResetEvent _lockOutOp;
        private readonly List<ServerEvent> _inEvents;
        private Task? _inTask;
        private LifecycleState _state;
        private bool _connected;
        private TcpClient? _client;
        private SslStream? _sslStream;
        private readonly Queue<ClientEvent> _writeEventQueue;
        private readonly ConcurrentQueue<ArraySegment<byte>> _writeQueue;

        /// <summary>
        /// Creates a new instance of <see cref="Client"/>.
        /// </summary>
        /// <param name="server">Server hostname.</param>
        /// <param name="port">Server port.</param>
        /// <param name="user">Username.</param>
        /// <param name="pass">Password.</param>
        /// <param name="registrationToken">User registration token.</param>
        /// <param name="logger">Logger implementation.</param>
        public Client(string server, ushort port, string user, string pass, string? registrationToken = null,
            ILogger? logger = null)
        {
            _logger = logger ?? NullLogger.Instance;
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
            _writeEventQueue = new Queue<ClientEvent>();
            _writeQueue = new ConcurrentQueue<ArraySegment<byte>>();
            OnReceivedEvent = _ => { };
            OnDisconnect = _ => { };
        }

        /// <summary>
        /// Attempts connection to server.
        /// </summary>
        /// <returns>Task</returns>
        /// <exception cref="LoginException">Invalid login was received.</exception>
        /// <exception cref="ProtocolException">Invalid event was received.</exception>
        /// <exception cref="SocketException">Socket failed to connect.</exception>
        /// <exception cref="InvalidOperationException">Operation not valid for current state of object.</exception>
        public async Task<UserInfoEvent> ConnectAsync()
        {
            await Task.Yield();
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
                Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Dispose, ref _state);
                throw;
            }

            try
            {
                var loginOp = Guid.NewGuid();
                var loginCommand = new LoginEvent { Operation = loginOp, User = User, Pass = _pass!, RegistrationToken = _registrationToken };
                WriteEvent(loginCommand);
                await FlushAsync(_cancellationTokenSource.Token).Caf();
                _pass = null;
                _registrationToken = null;
                var result = await WaitForAsync(e => e is IOperation op && op.Operation == loginOp, 10,
                    _cancellationTokenSource.Token).Caf();
                var info = result switch
                {
                    null => throw new ProtocolException(
                        "Server terminated the connection before a message was received."),
                    UserInfoEvent i => i,
                    LoginFailEvent => throw new LoginException("Login failed."),
                    _ => throw new ProtocolException($"Unexpected event type {result.GetType().FullName} received.")
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

                Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Dispose,
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

        /// <summary>
        /// Disconnects client and disposes this instance. Does not throw.
        /// </summary>
        /// <returns>Task representing this async operation.</returns>
        public async Task DisposeAsync()
        {
            if (_state == LifecycleState.Disposed) return;
            await Task.Yield();

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
            Dispose();
        }

        private static readonly Dictionary<LifecycleState, LifecycleState> _disposeMap =
            new()
            {
                { LifecycleState.NotStarted, LifecycleState.Disposed },
                { LifecycleState.Starting, LifecycleState.Dispose },
                { LifecycleState.Active, LifecycleState.Dispose },
                { LifecycleState.Dispose, LifecycleState.Dispose },
                { LifecycleState.Disposed, LifecycleState.Disposed }
            };

        private void Dispose()
        {
            _connected = false;

            Util.TriggerState(_op, _disposeMap, ref _state);
            if (_state == LifecycleState.Disposed) return;

            try
            {
                _sslStream?.Close();
            }
            catch
            {
                // ignored
            }
            finally
            {
                Util.TriggerState(_op, LifecycleState.Dispose, LifecycleState.Dispose, LifecycleState.Disposed,
                    ref _state);
            }
        }

        private bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            _logger.LogError(
                "Certificate error: {PolicyErrors}\nCertificate issuer: {Issuer}\nCertificate subject: {Subject}",
                sslPolicyErrors, certificate.Issuer, certificate.Subject);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        /// <inheritdoc />
        public Task<ServerEvent?> WaitForAsync(Func<ServerEvent, bool> predicate, int pollMillis) =>
            WaitForAsync(predicate, pollMillis, CancellationToken.None);

        /// <inheritdoc />
        public async Task<ServerEvent?> WaitForAsync(Func<ServerEvent, bool> predicate, int pollMillis,
            CancellationToken cancellationToken)
        {
            Util.RequireState(_state, LifecycleState.Starting, LifecycleState.Active);
            while (!cancellationToken.IsCancellationRequested)
            {
                _lockInOp.WaitOne();
                var evt = _inEvents.FirstOrDefault(predicate);
                if (evt != null) _inEvents.Remove(evt);
                _lockInOp.Set();
                if (evt != null) return evt;
                if (_inTask!.IsFaulted)
                    throw new Exception($"Could not read event: task excepted. Information:\n{_inTask.Exception}");
                if (_inTask.IsCompleted) return null;
                await Task.Delay(pollMillis, cancellationToken).Caf();
            }

            throw new TaskCanceledException();
        }

        /// <inheritdoc />
        public IEnumerable<ServerEvent> GetEvents(ICollection<ServerEvent>? output = null)
        {
            _lockInOp.WaitOne();
            if (output == null)
                output = new List<ServerEvent>(_inEvents);
            else
                foreach (var evt in _inEvents)
                    output.Add(evt);
            _inEvents.Clear();
            _lockInOp.Set();
            return output;
        }

        /// <inheritdoc />
        public void DiscardEvents()
        {
            _lockInOp.WaitOne();
            _inEvents.Clear();
            _lockInOp.Set();
        }

        /// <inheritdoc />
        public void WriteEvent(ClientEvent evt)
        {
            Util.RequireState(_state, LifecycleState.Starting, LifecycleState.Active);
            lock (_writeEventQueue)
                _writeEventQueue.Enqueue(evt);
        }

        /// <inheritdoc />
        public void WriteEvents(IEnumerable<ClientEvent> events)
        {
            Util.RequireState(_state, LifecycleState.Starting, LifecycleState.Active);
            lock (_writeEventQueue)
                foreach (var evt in events)
                    _writeEventQueue.Enqueue(evt);
        }

        /// <inheritdoc />
        public Task FlushAsync() => FlushAsync(CancellationToken.None);

        /// <inheritdoc />
        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            Util.RequireState(_state, LifecycleState.Starting, LifecycleState.Active);
            await Task.Yield();

            var ms = new MemoryStream();
            lock (_writeEventQueue)
            {
                while (_writeEventQueue.Count != 0)
                    ms.WriteEvent(_writeEventQueue.Dequeue());
                ms.TryGetBuffer(out ArraySegment<byte> buf);
                _writeQueue.Enqueue(buf);
            }

            _lockOutOp.WaitOne();
            try
            {
                while (_writeQueue.TryDequeue(out var segment))
                {
                    await _sslStream!.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken).Caf();
                    await _sslStream.FlushAsync(cancellationToken).Caf();
                }
            }
            finally
            {
                _lockOutOp.Set();
            }
        }

        /// <summary>
        /// If true, this object has been disposed.
        /// </summary>
        public bool Disposed => _state >= LifecycleState.Dispose;

        /// <inheritdoc />
        public bool Connected => _connected;

        /// <summary>
        /// Exception thrown when login has failed according to server response.
        /// </summary>
        public class LoginException : Exception
        {
            /// <summary>
            /// Create new instance of <see cref="LoginException"/>
            /// </summary>
            /// <param name="message">Description message.</param>
            public LoginException(string message) : base(message)
            {
            }
        }
    }
}

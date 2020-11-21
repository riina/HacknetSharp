using System;
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

namespace HacknetSharp.Client
{
    public class Connection : IConnection<ClientEvent, ServerEvent>
    {
        private readonly string _server;
        private readonly ushort _port;
        private readonly string _user;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private string? _pass;
        private readonly CountdownEvent _countdown;
        private readonly AutoResetEvent _op;
        private readonly AutoResetEvent _lockInOp;
        private readonly AutoResetEvent _lockOutOp;
        private readonly ManualResetEvent _readyOp;
        private readonly AutoResetEvent _outOp;
        private readonly List<ServerEvent> _inEvents;
        private readonly List<ClientEvent> _outEvents;
        private TcpClient? _client;
        private LifecycleState _state;
        private bool _closed;
        private SslStream? _stream;
        private BufferedStream? _bs;
        public Task ExecutionTask { get; }

        public Connection(string server, ushort port, string user, string pass)
        {
            _server = server;
            _port = port;
            _user = user;
            _cancellationTokenSource = new CancellationTokenSource();
            _pass = pass;
            _countdown = new CountdownEvent(1);
            _op = new AutoResetEvent(true);
            _readyOp = new ManualResetEvent(false);
            _outOp = new AutoResetEvent(false);
            _lockInOp = new AutoResetEvent(true);
            _lockOutOp = new AutoResetEvent(true);
            _state = LifecycleState.NotStarted;
            _inEvents = new List<ServerEvent>();
            _outEvents = new List<ClientEvent>();
            ExecutionTask = Task.Run(async () => await ExecuteSend(_cancellationTokenSource.Token));
            ExecutionTask = Task.Run(async () => await ExecuteReceive(_cancellationTokenSource.Token));
        }

        public async Task ConnectAsync()
        {
            Util.TriggerState(_op, LifecycleState.NotStarted, LifecycleState.NotStarted, LifecycleState.Starting,
                ref _state);
            try
            {
                var client = new TcpClient(_server, _port);
                _stream = new SslStream(
                    client.GetStream(), false, ValidateServerCertificate
                );
                await _stream.AuthenticateAsClientAsync(_server, default, SslProtocols.Tls12, true);
                _bs = new BufferedStream(_stream);
                _readyOp.Set();
            }
            catch
            {
                Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Failed,
                    ref _state);
                throw;
            }

            try
            {
                var loginCommand = new LoginEvent {User = _user, Pass = _pass};
                WriteEvent(loginCommand);
                _pass = null;
                var result = await WaitForAsync(_ => true, 10, _cancellationTokenSource.Token);
                switch (result)
                {
                    case UserInfoEvent info:
                    {
                        Console.WriteLine("Login successful.");
                        break;
                    }
                    case LoginFailEvent fail:
                    {
                        throw new LoginException("Login failed.");
                    }
                    default:
                        throw new ProtocolException($"Unexpected event type {result.GetType().FullName} received.");
                }
            }
            catch
            {
                WriteEvent(ClientDisconnectEvent.Singleton);
                Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Failed,
                    ref _state);
                throw;
            }

            Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Active, ref _state);
        }

        private async Task ExecuteSend(CancellationToken cancellationToken)
        {
            _readyOp.WaitOne();
            if (_stream == null) return;
            if (_bs == null) return;
            while (!cancellationToken.IsCancellationRequested)
            {
                _outOp.WaitOne();
                _lockInOp.WaitOne();
                foreach (var evt in _outEvents)
                    _bs.WriteEvent(evt);
                _outEvents.Clear();
                _lockInOp.Set();
                await _bs.FlushAsync(cancellationToken);
            }

            throw new TaskCanceledException();
        }

        private async Task ExecuteReceive(CancellationToken cancellationToken)
        {
            _readyOp.WaitOne();
            while (!cancellationToken.IsCancellationRequested)
            {
                var evt = _bs.ReadEvent<ServerEvent>();
                _lockInOp.WaitOne();
                _inEvents.Add(evt);
                _lockInOp.Set();
            }

            throw new TaskCanceledException();
        }

        public async Task DisposeAsync()
        {
            if (_state == LifecycleState.NotStarted)
            {
                _state = LifecycleState.Disposed;
                return;
            }

            Util.RequireState(_state, LifecycleState.Starting, LifecycleState.Active);
            _cancellationTokenSource.Cancel();
            while (_state != LifecycleState.Active && _state != LifecycleState.Failed) await Task.Delay(100).Caf();
            try
            {
                CloseStream();
            }
            catch
            {
                // ignored
            }

            if (_state == LifecycleState.Failed) return;
            Util.TriggerState(_op, LifecycleState.Active, LifecycleState.Active, LifecycleState.Dispose, ref _state);

            await Task.Run(() =>
            {
                _op.WaitOne();
                _countdown.Signal();
                _op.Set();
                _countdown.Wait();
            });
            Util.TriggerState(_op, LifecycleState.Dispose, LifecycleState.Dispose, LifecycleState.Disposed, ref _state);
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

        private void CloseStream()
        {
            lock (_cancellationTokenSource)
                if (!_closed)
                {
                    _stream?.Close();
                    _closed = true;
                }
        }

        public async Task<ServerEvent> WaitForAsync(Func<ServerEvent, bool> predicate, int pollMillis,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _lockInOp.WaitOne();
                var evt = _inEvents.FirstOrDefault(predicate);
                if (evt != null) _inEvents.Remove(evt);
                _lockInOp.Set();
                if (evt != null) return evt;
                await Task.Delay(pollMillis, cancellationToken);
            }

            throw new TaskCanceledException();
        }

        public IEnumerable<ServerEvent> ReadEvents()
        {
            _lockInOp.WaitOne();
            var list = new List<ServerEvent>(_inEvents);
            _inEvents.Clear();
            _lockOutOp.Set();
            return list;
        }

        public void WriteEvent(ClientEvent evt)
        {
            _lockOutOp.WaitOne();
            _outEvents.Add(evt);
            _lockOutOp.Set();
            _outOp.Set();
        }

        public void WriteEvents(IEnumerable<ClientEvent> events)
        {
            _lockOutOp.WaitOne();
            _outEvents.AddRange(events);
            _lockOutOp.Set();
            _outOp.Set();
        }
    }
}

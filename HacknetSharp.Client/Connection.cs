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
        private readonly List<ServerEvent> _inEvents;
        private readonly Task _inTask;
        private LifecycleState _state;
        private bool _closed;
        private SslStream? _inStream;
        private BufferedStream? _outStream;

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
            _lockInOp = new AutoResetEvent(true);
            _lockOutOp = new AutoResetEvent(true);
            _state = LifecycleState.NotStarted;
            _inEvents = new List<ServerEvent>();
            _inTask = Task.Run(async () => await ExecuteReceive(_cancellationTokenSource.Token));
        }

        public async Task ConnectAsync()
        {
            Util.TriggerState(_op, LifecycleState.NotStarted, LifecycleState.NotStarted, LifecycleState.Starting,
                ref _state);
            try
            {
                var client = new TcpClient(_server, _port);
                _inStream = new SslStream(
                    client.GetStream(), false, ValidateServerCertificate
                );
                await _inStream.AuthenticateAsClientAsync(_server, default, SslProtocols.Tls12, true);
                _outStream = new BufferedStream(_inStream);
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
                var loginCommand = new LoginEvent {User = _user, Pass = _pass!};
                WriteEvent(loginCommand);
                await FlushAsync(_cancellationTokenSource.Token);
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
                        throw new ProtocolException($"Unexpected event type {result?.GetType().FullName} received.");
                }
            }
            catch
            {
                try
                {
                    WriteEvent(ClientDisconnectEvent.Singleton);
                    await FlushAsync(_cancellationTokenSource.Token);
                }
                catch
                {
                    // ignored
                }

                Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Failed,
                    ref _state);
                throw;
            }

            Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Active, ref _state);
        }

        private async Task ExecuteReceive(CancellationToken cancellationToken)
        {
            _readyOp.WaitOne();
            if (_inStream == null) return;
            if (_outStream == null) return;
            while (!cancellationToken.IsCancellationRequested)
            {
                var evt = await _inStream.ReadEventAsync<ServerEvent>(cancellationToken);
                if (evt == null) return;
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
                    _inStream?.Close();
                    _closed = true;
                }
        }

        public Task<ServerEvent?> WaitForAsync(Func<ServerEvent, bool> predicate, int pollMillis) =>
            WaitForAsync(predicate, pollMillis, CancellationToken.None);

        public async Task<ServerEvent?> WaitForAsync(Func<ServerEvent, bool> predicate, int pollMillis,
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _lockInOp.WaitOne();
                var evt = _inEvents.FirstOrDefault(predicate);
                if (evt != null) _inEvents.Remove(evt);
                _lockInOp.Set();
                if (evt != null) return evt;
                if (_inTask.IsFaulted)
                    throw new Exception($"Could not read event: task excepted. Information:\n{_inTask.Exception}");
                if (_inTask.IsCompleted)
                    throw new Exception("Read task has completed, cannot receive any events.");
                await Task.Delay(pollMillis, cancellationToken);
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
            if (_outStream == null) throw new InvalidOperationException();
            _lockOutOp.WaitOne();
            try
            {
                _outStream.WriteEvent(evt);
            }
            finally
            {
                _lockOutOp.Set();
            }
        }

        public void WriteEvents(IEnumerable<ClientEvent> events)
        {
            if (_outStream == null) throw new InvalidOperationException();
            _lockOutOp.WaitOne();
            try
            {
                foreach (var evt in events) _outStream.WriteEvent(evt);
            }
            finally
            {
                _lockOutOp.Set();
            }
        }

        public Task FlushAsync() => FlushAsync(CancellationToken.None);

        public async Task FlushAsync(CancellationToken cancellationToken)
        {
            if (!_closed && _outStream != null)
            {
                _lockOutOp.WaitOne();
                await _outStream.FlushAsync(cancellationToken);
                _lockOutOp.Set();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp.Events.Client;
using Ns;

namespace HacknetSharp.Client
{
    public class Connection : IConnection<ClientEvent, ServerEvent>
    {
        private readonly string _server;
        private readonly ushort _port;
        private readonly string _user;
        private string? _pass;
        private readonly CountdownEvent _countdown;
        private readonly AutoResetEvent _op;
        private LifecycleState _state;

        public Connection(string server, ushort port, string user, string pass)
        {
            _server = server;
            _port = port;
            _user = user;
            _pass = pass;
            _countdown = new CountdownEvent(1);
            _op = new AutoResetEvent(true);
            _state = LifecycleState.NotStarted;
        }

        public async Task ConnectAsync()
        {
            Util.TriggerState(_op, LifecycleState.NotStarted, LifecycleState.NotStarted, LifecycleState.Starting,
                ref _state);
            try
            {
                var client = new TcpClient(_server, _port);
                using var sslStream = new SslStream(
                    client.GetStream(), false, ValidateServerCertificate
                );
                await sslStream.AuthenticateAsClientAsync(_server, default, SslProtocols.Tls12, true);
                var bs = new BufferedStream(sslStream);
                var loginCommand = new LoginEvent{User=_user,Pass=_pass};
                bs.WriteEvent(loginCommand);
                _pass = null;
                await bs.FlushAsync();
                if (!bs.Expect(ServerClientCommand.UserInfo, out var loginRes))
                    throw loginRes switch
                    {
                        ServerClientCommand.LoginFail => new LoginException("Login failed."),
                        _ => ProtocolException.FromUnexpectedCommand(loginRes)
                    };
            }
            catch
            {
                Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Failed,
                    ref _state);
                throw;
            }

            Util.TriggerState(_op, LifecycleState.Starting, LifecycleState.Starting, LifecycleState.Active, ref _state);
        }

        public async Task DisposeAsync()
        {
            Util.RequireState(_state, LifecycleState.Starting, LifecycleState.Active);
            while (_state != LifecycleState.Active) await Task.Delay(100).Caf();
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

        public ServerEvent WaitFor(Predicate<ServerEvent> predicate) => throw new NotImplementedException();

        public IEnumerable<ServerEvent> ReadEvents() => throw new NotImplementedException();

        public void WriteEvents(IEnumerable<ClientEvent> events) => throw new NotImplementedException();
    }
}

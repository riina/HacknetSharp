using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Ns;

namespace HacknetSharp.Client
{
    public class Connection
    {
        private readonly string _server;
        private readonly ushort _port;
        private readonly string _user;
        private string _pass;

        public Connection(string server, ushort port, string user, string pass)
        {
            _server = server;
            _port = port;
            _user = user;
            _pass = pass;
        }

        public async Task ConnectAsync()
        {

            var client = new TcpClient(_server, _port);
            using var sslStream = new SslStream(
                client.GetStream(), false, ValidateServerCertificate
            );
            await sslStream.AuthenticateAsClientAsync(_server, default, SslProtocols.Tls12, true);

            // TODO implement handshake + user credentials
            sslStream.WriteUtf8String("user here");
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
    }
}

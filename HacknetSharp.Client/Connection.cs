using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Ns;

namespace HacknetSharp.Client
{
    public class Connection
    {
        public Connection(string server, ushort port)
        {
            var client = new TcpClient(server, port);
            using var sslStream = new SslStream(
                client.GetStream(), false, ValidateServerCertificate
            );
            sslStream.AuthenticateAsClient(server, default, SslProtocols.Default, true);

            // TODO implement handshake + user credentials
            var ns = new NetSerializer(sslStream);
            ns.WriteUtf8String("user here");

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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp;
using HacknetSharp.Client;
using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;

namespace hsc
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    internal static class Program
    {
        private static async Task Main(string[] args)
            => await Parser.Default
                .ParseArguments<ForgeTokenOptions, LoginOptions
                >(args)
                .MapResult<ForgeTokenOptions, LoginOptions, Task<int>>(
                    RunForgeToken, RunLogin,
                    errs => Task.FromResult(1));

        private static readonly Regex _conStringRegex = new Regex(@"([A-Za-z0-9]+)@([\S]+)");
        private static readonly Regex _serverPortRegex = new Regex(@"([^\s:]+):([\S]+)");

        [Verb("forgetoken", HelpText = "make registration token on server")]
        private class ForgeTokenOptions
        {
            [Value(0, MetaName = "conString", HelpText = "Connection string (user@server[:port])", Required = true)]
            public string ConString { get; set; } = null!;
        }

        private static async Task<int> RunForgeToken(ForgeTokenOptions options)
        {
            var connection = GetConnection(options.ConString, true, out (int, string)? failReason);
            if (connection == null)
            {
                if (!failReason.HasValue) return 0;
                Console.WriteLine(failReason.Value.Item2);
                return failReason.Value.Item1;
            }

            UserInfoEvent userInfoEvent;
            try
            {
                userInfoEvent = await connection.ConnectAsync();
            }
            catch (LoginException)
            {
                Console.WriteLine("Login failed.");
                return 0x20201;
            }
            catch (ProtocolException e)
            {
                Console.WriteLine($"A protocol error occurred: {e.Message}");
                return 0x10101;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An unknown error occurred: {e}.");
                return 0x1;
            }

            Console.WriteLine($"Logged in as {connection.User} ({(userInfoEvent.Admin ? "admin" : "normal user")})");
            if (!userInfoEvent.Admin)
            {
                Console.WriteLine("Non-admin users cannot forge tokens.");
                Console.WriteLine(0x20202);
            }

            var operation = Guid.NewGuid();
            connection.WriteEvent(new RegistrationTokenForgeRequestEvent {Operation = operation});
            await connection.FlushAsync();
            var response = await connection.WaitForAsync(
                e => (e is FailBaseServerEvent fail && fail.Operation == operation) ||
                     e is RegistrationTokenForgeResponseEvent,
                10);
            if (response is FailBaseServerEvent failResponse)
            {
                Console.WriteLine($"Server returned an error: {failResponse.Message}");
            }
            else if (response is RegistrationTokenForgeResponseEvent regResponse)
            {
                Console.WriteLine($"TOKEN: {regResponse.RegistrationToken}");
            }

            return 0;
        }

        [Verb("login", HelpText = "login to server")]
        private class LoginOptions
        {
            [Value(0, MetaName = "conString", HelpText = "Connection string (user@server[:port])", Required = true)]
            public string ConString { get; set; } = null!;

            [Option('r', "register", HelpText = "Register on server (requires registration token).")]
            public bool Register { get; set; }
        }

        private static async Task<int> RunLogin(LoginOptions options)
        {
            var connection = GetConnection(options.ConString, options.Register, out (int, string)? failReason);
            if (connection != null)
                return await ExecuteClient(connection);

            if (!failReason.HasValue) return 0;
            Console.WriteLine(failReason.Value.Item2);
            return failReason.Value.Item1;
        }

        private static async Task<int> ExecuteClient(ClientConnection connection)
        {
            UserInfoEvent userInfoEvent;
            try
            {
                userInfoEvent = await connection.ConnectAsync();
            }
            catch (LoginException)
            {
                Console.WriteLine("Login failed.");
                return 0x20201;
            }
            catch (ProtocolException e)
            {
                Console.WriteLine($"A protocol error occurred: {e.Message}");
                return 0x10101;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An unknown error occurred: {e}.");
                return 0x1;
            }

            Console.WriteLine($"Logged in as {connection.User} ({(userInfoEvent.Admin ? "admin" : "normal user")})");

            // TODO client things, this is temporary
            connection.WriteEvent(ClientDisconnectEvent.Singleton);
            await connection.FlushAsync();
            return 0;
        }

        private static ClientConnection? GetConnection(string conString, bool askRegistrationToken,
            out (int, string)? failReason)
        {
            failReason = null;
            var conStringMatch = _conStringRegex.Match(conString);
            if (!conStringMatch.Success)
            {
                failReason = (0x80801, "Invalid constring, must be user@server[:port]");
                return null;
            }

            string user = conStringMatch.Groups[1].Value;
            string server = conStringMatch.Groups[2].Value;
            ushort port = Constants.DefaultPort;
            if (server.Contains(':'))
            {
                var serverPortMatch = _serverPortRegex.Match(server);
                if (!serverPortMatch.Success || !ushort.TryParse(serverPortMatch.Groups[2].Value, out port))
                {
                    failReason = (0x80802, "Invalid server/port, must be user@server[:port]");
                    return null;
                }

                server = serverPortMatch.Groups[1].Value;
            }

            string? pass = Util.PromptPassword("Pass:");
            string? registrationToken = askRegistrationToken ? Util.PromptPassword("Registration Token:") : null;
            return pass != null ? new ClientConnection(server, port, user, pass, registrationToken) : null;
        }
    }
}

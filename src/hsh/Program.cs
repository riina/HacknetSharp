using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp;
using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;

namespace hsh
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    internal static class Program
    {
        private static async Task Main(string[] args)
            => await Parser.Default
                .ParseArguments<Options>(args)
                .MapResult(Run, errs => Task.FromResult(1)).Caf();

        private class Options
        {
            [Option('f', "forgetoken", HelpText = "Make registration token on server.", SetName = "forgetoken")]
            public bool ForgeToken { get; set; }

            [Option('r', "register", HelpText = "Register on server (requires registration token).",
                SetName = "register")]
            public bool Register { get; set; }

            [Value(0, MetaName = "conString", HelpText = "Connection string (user@server[:port])", Required = true)]
            public string ConString { get; set; } = null!;
        }

        private static async Task<int> Run(Options options)
        {
            var connection = GetConnection(options.ConString, options.Register, out (int, string)? failReason);
            if (connection != null)
                return options.ForgeToken ? await ForgeToken(connection).Caf() : await ExecuteClient(connection).Caf();

            if (!failReason.HasValue) return 0;
            Console.WriteLine(failReason.Value.Item2);
            return failReason.Value.Item1;
        }

        private static async Task<int> ForgeToken(Client connection)
        {
            (UserInfoEvent? user, int resCode) = await Connect(connection).Caf();
            if (user == null) return resCode;

            Console.WriteLine($"Logged in as {connection.User} ({(user.Admin ? "admin" : "normal user")})");
            if (!user.Admin)
            {
                Console.WriteLine("Non-admin users cannot forge tokens.");
                Console.WriteLine(0x20202);
            }

            var operation = Guid.NewGuid();
            connection.WriteEvent(new RegistrationTokenForgeRequestEvent {Operation = operation});
            await connection.FlushAsync().Caf();
            var response = await connection.WaitForAsync(
                e => e is IOperation op && op.Operation == operation,
                10).Caf();
            if (response == null)
            {
                Console.WriteLine("No response received from server.");
                return 0x10102;
            }

            switch (response)
            {
                case FailBaseServerEvent failResponse:
                    Console.WriteLine($"Server returned an error: {failResponse.Message}");
                    return 0x1;
                case RegistrationTokenForgeResponseEvent regResponse:
                    Console.WriteLine($"TOKEN: {regResponse.RegistrationToken}");
                    return 0;
                default:
                    Console.WriteLine($"Unexpected event {response.GetType().FullName}");
                    return 0x10101;
            }
        }

        private static async Task<int> ExecuteClient(Client connection)
        {
            connection.OnReceivedEvent += e =>
            {
                switch (e)
                {
                    case OutputEvent output:
                        Console.Write(output.Text);
                        break;
                    case InputRequestEvent inputRequest:
                        connection.WriteEvent(new InputResponseEvent
                        {
                            Operation = inputRequest.Operation, Input = Util.ReadPassword() ?? ""
                        });
                        connection.FlushAsync();
                        break;
                }
            };
            connection.OnDisconnect += e =>
            {
                Console.WriteLine($"Disconnected by server. Reason: {e.Reason}");
                try
                {
                    connection.DisposeAsync().Wait();
                }
                catch
                {
                    // ignored
                }

                Environment.Exit(0);
            };
            (UserInfoEvent? user, int resCode) = await Connect(connection).Caf();
            if (user == null) return resCode;
            var operation = Guid.NewGuid();
            connection.WriteEvent(new InitialCommandEvent {Operation = operation, ConWidth = Console.WindowWidth});
            await connection.FlushAsync().Caf();
            do
            {
                var operationLcl = operation;
                ServerEvent? endEvt = await connection.WaitForAsync(
                    e => e is IOperation op && op.Operation == operationLcl, 10).Caf();
                if (endEvt == null) break;
                operation = Guid.NewGuid();
                connection.WriteEvent(new CommandEvent
                {
                    Operation = operation,
                    ConWidth = Console.WindowWidth,
                    Text = Console.ReadLine() ?? throw new ApplicationException()
                });

                await connection.FlushAsync().Caf();
            } while (true);

            await connection.DisposeAsync();
            return 0;
        }

        private static async Task<(UserInfoEvent?, int)> Connect(Client connection)
        {
            UserInfoEvent userInfoEvent;
            try
            {
                userInfoEvent = await connection.ConnectAsync().Caf();
            }
            catch (Client.LoginException)
            {
                Console.WriteLine("Login failed.");
                return (null, 0x20201);
            }
            catch (ProtocolException e)
            {
                Console.WriteLine($"A protocol error occurred: {e.Message}");
                return (null, 0x10101);
            }
            catch (Exception e)
            {
                Console.WriteLine($"An unknown error occurred: {e}.");
                return (null, 0x1);
            }

            //Console.WriteLine($"Logged in as {connection.User} ({(userInfoEvent.Admin ? "admin" : "normal user")})");
            return (userInfoEvent, 0);
        }

        private static Client? GetConnection(string conString, bool askRegistrationToken,
            out (int, string)? failReason)
        {
            failReason = null;
            if (!Util.TryParseConString(conString, Constants.DefaultPort, out string? user, out string? server,
                out ushort port, out string? error))
            {
                failReason = (101, error ?? "");
                return null;
            }

            Console.Write("Pass:");
            string? pass = Util.ReadPassword();
            string? registrationToken;
            if (askRegistrationToken)
            {
                Console.Write("Registration Token:");
                registrationToken = Util.ReadPassword();
            }
            else
                registrationToken = null;

            return pass != null ? new Client(server!, port, user!, pass, registrationToken) : null;
        }
    }
}

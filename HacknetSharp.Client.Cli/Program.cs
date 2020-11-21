using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;

namespace HacknetSharp.Client.Cli
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: <server> <user>");
                return;
            }

            string? pass = PromptSecureString("Pass:");
            if (pass == null) return;
            var connection = new Connection(args[0], 42069, args[1], pass);
            try
            {
                await connection.ConnectAsync();
            }
            catch (LoginException)
            {
                Console.WriteLine("Login failed.");
                return;
            }
            catch (ProtocolException e)
            {
                Console.WriteLine($"A protocol error occurred: {e.Message}");
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine($"An unknown error occurred: {e}.");
                return;
            }

            // TODO client things
            connection.WriteEvent(new CommandEvent {Text = "input"});
            var res = (await connection.WaitForAsync(e => e is OutputEvent, 10) as OutputEvent)!;
            Console.WriteLine($"Received: {res.Text}");
        }

        public static string? PromptSecureString(string mes)
        {
            Console.Write(mes);

            var ss = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.C && (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control)
                {
                    return null;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (ss.Length != 0)
                        ss.Remove(ss.Length - 1, 1);
                    continue;
                }

                ss.Append(key.KeyChar);
            }

            Console.WriteLine();
            return ss.ToString();
        }
    }
}

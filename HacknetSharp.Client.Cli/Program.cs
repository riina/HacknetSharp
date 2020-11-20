using System;
using System.Reflection.Metadata;
using System.Security;
using System.Text;
using System.Threading.Tasks;

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
                Console.WriteLine($"An unknown error occurred: {e.Message}.");
                return;
            }

            // TODO client things
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

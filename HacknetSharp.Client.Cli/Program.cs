using System;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace HacknetSharp.Client.Cli
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            string? pass = PromptSecureString("Pass:");
            if (pass == null) return;
            var connection = new Connection(args[0], 42069, args[1], pass);
            await connection.ConnectAsync();
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

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp;
using HacknetSharp.Client;
using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;

namespace hsc
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

            string? pass = Util.PromptPassword("Pass:");
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

            // TODO client things, this is temporary
            connection.WriteEvent(ClientDisconnectEvent.Singleton);
            await connection.FlushAsync();
            //var res = (await connection.WaitForAsync(e => e is OutputEvent, 10) as OutputEvent)!;
            //Console.WriteLine($"Received: {res.Text}");
        }
    }
}

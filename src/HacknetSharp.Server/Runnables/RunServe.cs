using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using CommandLine;

namespace HacknetSharp.Server.Runnables
{
    [Verb("serve", HelpText = "Serve content.")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    internal class RunServe : Executor.IRunnable
    {
        private class Options
        {
        }

        private static async Task<int> Start(Executor executor, Options options)
        {
            if (!File.Exists(ServerConstants.ServerYamlFile))
            {
                Console.WriteLine($"Could not find {ServerConstants.ServerYamlFile}");
                return 8;
            }

            var (_, servConf) = ServerUtil.ReadFromFile<ServerYaml>(ServerConstants.ServerYamlFile);
            if (servConf.Host == null)
            {
                Console.WriteLine($"Config has null {nameof(ServerYaml.Host)}");
                return 69;
            }

            if (servConf.DefaultWorld == null)
            {
                Console.WriteLine($"Config has null {nameof(ServerYaml.DefaultWorld)}");
                return 420;
            }

            Console.WriteLine("Looking for cert...");
            var cert = ServerUtil.FindCertificate(servConf.Host, ServerUtil.CertificateStores);
            if (cert == null)
            {
                Console.WriteLine("Failed to find certificate");
                return 304;
            }

            Console.WriteLine(
                $"Found cert in {cert.Value.Item1.Location}:{cert.Value.Item1.Location} - {cert.Value.Item2.Subject}");


            var conf = new ServerConfig()
                .WithPrograms(executor.CustomPrograms)
                .WithStorageContextFactory(executor.StorageContextFactory)
                .WithDefaultWorld(servConf.DefaultWorld)
                .WithPort(servConf.Port)
                .WithTemplates(ServerUtil.GetTemplates(""))
                .WithCertificate(cert.Value.Item2);
            var instance = conf.CreateInstance();
            await instance.StartAsync().Caf();

            // Block the program until it is closed.
            Console.WriteLine("Press Ctrl-C to terminate.");

            Console.TreatControlCAsInput = true;

            ConsoleKeyInfo ki;
            while ((ki = Console.ReadKey(true)).Key != ConsoleKey.C ||
                   (ki.Modifiers & ConsoleModifiers.Control) != ConsoleModifiers.Control)
            {
            }

            Console.TreatControlCAsInput = false;

            Console.WriteLine("Tearing down...");
            await instance.DisposeAsync().Caf();
            Console.WriteLine("Teardown complete.");
            return 0;
        }

        public async Task<int> Run(Executor executor, IEnumerable<string> args) => await Parser
            .Default.ParseArguments<Options>(args)
            .MapResult(x => Start(executor, x), x => Task.FromResult(1)).Caf();
    }
}

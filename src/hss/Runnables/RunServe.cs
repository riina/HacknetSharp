using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp;
using HacknetSharp.Server;

namespace hss.Runnables
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
            if (!File.Exists(HssConstants.ServerYamlFile))
            {
                Console.WriteLine($"Could not find {HssConstants.ServerYamlFile}");
                return 8;
            }

            var (_, servConf) = HssUtil.ReadFromFile<ServerSettings>(HssConstants.ServerYamlFile);
            if (servConf.Host == null)
            {
                Console.WriteLine($"Config has null {nameof(ServerSettings.Host)}");
                return 69;
            }

            if (servConf.DefaultWorld == null)
            {
                Console.WriteLine($"Config has null {nameof(ServerSettings.DefaultWorld)}");
                return 420;
            }

            Console.WriteLine("Looking for cert...");
            var cert = HssUtil.FindCertificate(servConf.Host, HssUtil.CertificateStores);
            if (cert == null)
            {
                Console.WriteLine("Failed to find certificate");
                return 304;
            }

            Console.WriteLine(
                $"Found cert in {cert.Value.Item1.Location}:{cert.Value.Item1.Location} - {cert.Value.Item2.Subject}");


            var conf = new ServerConfig()
                .WithPrograms(executor.ServerDatabaseContextFactory.Programs.Concat(executor.CustomPrograms))
                .WithServices(executor.ServerDatabaseContextFactory.Services.Concat(executor.CustomServices))
                .WithStorageContextFactory(executor.ServerDatabaseContextFactory)
                .WithDefaultWorld(servConf.DefaultWorld)
                .WithPort(servConf.Port)
                .WithTemplates(HssUtil.GetTemplates(""))
                .WithCertificate(cert.Value.Item2);
            var instance = conf.CreateInstance();
            _ = instance.Start();

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

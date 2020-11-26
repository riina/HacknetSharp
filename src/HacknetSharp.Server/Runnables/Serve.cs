using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;

namespace HacknetSharp.Server.Runnables
{
    [Verb("serve", HelpText = "Serve content.")]
    internal class Serve<TDatabaseFactory> : Executor<TDatabaseFactory>.IRunnable
        where TDatabaseFactory : StorageContextFactoryBase
    {
        private class Options
        {
            [Value(0, MetaName = "externalAddr", HelpText = "External address.", Required = true)]
            public string ExternalAddr { get; set; } = null!;

            [Value(1, MetaName = "defaultWorld", HelpText = "Default world to load.", Required = true)]
            public string DefaultWorld { get; set; } = null!;
        }

        private static async Task<int> Start(Executor<TDatabaseFactory> executor, Options options)
        {
            Console.WriteLine("Looking for cert...");
            var cert = ServerUtil.FindCertificate(options.ExternalAddr, ServerUtil.CertificateStores);
            if (cert == null)
            {
                Console.WriteLine("Failed to find certificate");
                return 304;
            }

            Console.WriteLine(
                $"Found cert in {cert.Value.Item1.Location}:{cert.Value.Item1.Location} - {cert.Value.Item2.Subject}");


            var conf = new ServerConfig()
                .WithPrograms(executor.CustomPrograms)
                .WithStorageContextFactory<TDatabaseFactory>()
                .WithAccessController<AccessController>()
                .WithDefaultWorld(options.DefaultWorld)
                .WithPort(42069)
                .WithCertificate(cert.Value.Item2);
            if (Directory.Exists(ServerConstants.WorldTemplatesFolder))
                conf.WithWorldTemplates(Directory.EnumerateFiles(ServerConstants.WorldTemplatesFolder)
                    .Select(ReadFromFile<WorldTemplate>));
            if (Directory.Exists(ServerConstants.SystemTemplatesFolder))
                conf.WithSystemTemplates(Directory.EnumerateFiles(ServerConstants.SystemTemplatesFolder)
                    .Select(ReadFromFile<SystemTemplate>));
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

            Console.WriteLine("Tearing down...");
            await instance.DisposeAsync().Caf();
            Console.WriteLine("Teardown complete.");
            return 0;
        }

        private static T ReadFromFile<T>(string file) =>
            ServerUtil.YamlDeserializer.Deserialize<T>(File.ReadAllText(file));

        public async Task<int> Run(Executor<TDatabaseFactory> executor, IEnumerable<string> args) => await Parser
            .Default.ParseArguments<Options>(args)
            .MapResult(x => Start(executor, x), x => Task.FromResult(1)).Caf();
    }
}

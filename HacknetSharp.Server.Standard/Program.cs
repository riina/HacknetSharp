using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp.Server.Postgres;
using YamlDotNet.Serialization;

namespace HacknetSharp.Server.Standard
{
    internal static class Program
    {
        static Program()
        {
            _models = new HashSet<Type[]>();
            _programs = new HashSet<Type[]>();
            Util.LoadTypesFromFolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ExtensionsFolder), _models,
                _programs);
        }

        private const string ExtensionsFolder = "extensions";

        private class StandardPostgresStorageContextFactory : PostgresStorageContextFactory
        {
            protected override IEnumerable<IEnumerable<Type>> CustomModelsMulti => _models;
        }

        private class StandardAccessController : AccessController
        {
            public override bool Authenticate(string user, string pass)
            {
                throw new NotImplementedException();
            }

            public override void Register(string user, string pass, string adminUser)
            {
                throw new NotImplementedException();
            }

            public override void Deregister(string user, string pass, string adminUser, bool purge)
            {
                throw new NotImplementedException();
            }
        }

        private static readonly HashSet<Type[]> _models;
        private static readonly HashSet<Type[]> _programs;

        private static async Task<int> Main(string[] args) =>
            await Parser.Default
                .ParseArguments<CreateOptions, RunOptions
                >(args)
                .MapResult<CreateOptions, RunOptions, Task<int>>(
                    RunCreate,
                    RunRun,
                    errs => Task.FromResult(1));

        // TODO purge option (clear database of content outside specified worlds)

        [Verb("create", HelpText = "create world configuration")]
        private class CreateOptions
        {
            [Value(0, MetaName = "worldName", HelpText = "Name of world", Required = true)]
            public string WorldName { get; set; }

            [Option('f', "force", HelpText = "Force overwrite existing config.")]
            public bool Force { get; set; }
        }

        private static Task<int> RunCreate(CreateOptions options)
        {
            var worldConfig = new WorldConfig {Name = options.WorldName, DatabaseKey = Guid.NewGuid()};
            string name = $"{options.WorldName}.yaml";
            if (File.Exists(name) && !options.Force)
            {
                Console.WriteLine($"A configuration file named {name} already exists. Use -f/--force to overwrite it.");
                return Task.FromResult(3);
            }

            using var tw = new StreamWriter(File.OpenWrite(name));
            _serializer.Serialize(tw, worldConfig);
            return Task.FromResult(0);
        }

        [Verb("run", HelpText = "run server")]
        private class RunOptions
        {
            [Value(0, MetaName = "worldConfigs", HelpText = "World configuration YAML files.")]
            public IEnumerable<string> WorldConfigs { get; set; }
        }

        private static async Task<int> RunRun(RunOptions options)
        {
            // TODO maybe programs -> types (declare with attrs)

            var instance = new ServerConfig()
                .WithModels(_models)
                .WithPrograms(_programs)
                .WithStorageContextFactory<StandardPostgresStorageContextFactory>()
                .WithAccessController<StandardAccessController>()
                .WithWorldConfigs(options.WorldConfigs.Select(ReadWorldConfigFromFile))
                .CreateInstance();
            await instance.StartAsync();

            // Block the program until it is closed.
            Console.WriteLine("Press Ctrl-C to terminate.");

            Console.TreatControlCAsInput = true;

            ConsoleKeyInfo ki;
            while ((ki = Console.ReadKey(true)).Key != ConsoleKey.C ||
                   (ki.Modifiers & ConsoleModifiers.Control) != ConsoleModifiers.Control)
            {
            }

            Console.WriteLine("Tearing down...");
            await instance.DisposeAsync();
            Console.WriteLine("Teardown complete.");
            return 0;
        }

        private static readonly IDeserializer _deserializer = new DeserializerBuilder().Build();
        private static readonly ISerializer _serializer = new SerializerBuilder().Build();

        private static WorldConfig ReadWorldConfigFromFile(string file) =>
            _deserializer.Deserialize<WorldConfig>(File.ReadAllText(file));
    }
}

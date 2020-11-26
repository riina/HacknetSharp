using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CommandLine;

namespace HacknetSharp.Server.Runnables
{
    [Verb("new", HelpText = "Create template.")]
    internal class New<TDatabaseFactory> : Executor<TDatabaseFactory>.IRunnable
        where TDatabaseFactory : StorageContextFactoryBase
    {
        private class Options
        {
            [Value(0, MetaName = "kind", HelpText = "Kind of template.", Required = true)]
            public string Kind { get; set; } = null!;

            [Value(1, MetaName = "name", HelpText = "Name for template instance.", Required = true)]
            public string Name { get; set; } = null!;

            [Option('f', "force", HelpText = "Force overwrite existing template.")]
            public bool Force { get; set; }
        }

        private static readonly Dictionary<string, Func<Options, (string path, object result)>>
            _templateGenerators =
                new Dictionary<string, Func<Options, (string path, object result)>>
                {
                    {
                        "system", options => (
                            Path.Combine(ServerConstants.SystemTemplatesFolder, $"{options.Name}.yaml"),
                            (object)new SystemTemplate
                            {
                                OsName = "EncomOS",
                                Users = new List<string>(new[] {"{u}+:{p}", "samwise:genshin"}),
                                Filesystem = new List<string>(new[]
                                {
                                    "fold*+*:/bin", "fold:/etc", "fold:/home", "fold*+*:/lib", "fold:/mnt",
                                    "fold+++:/root", "fold:/usr", "fold:/usr/bin", "fold:/usr/lib",
                                    "fold:/usr/local", "fold:/usr/share", "fold:/var", "fold:/var/spool",
                                    "text:\"/home/samwise/read me.txt\" mr. frodo, sir!",
                                    "file:/home/samwise/image.png misc/image.png", "prog:/bin/cat core:cat",
                                    "prog:/bin/cd core:cd", "prog:/bin/ls core:ls"
                                })
                            })
                    },
                    {
                        "world", options => (
                            Path.Combine(ServerConstants.WorldTemplatesFolder, $"{options.Name}.yaml"),
                            (object)new WorldTemplate {Name = options.Name})
                    }
                };

        private static Task<int> Start(Executor<TDatabaseFactory> executor, Options options)
        {
            if (!_templateGenerators.TryGetValue(options.Kind.ToLowerInvariant(), out var action))
            {
                Console.WriteLine("Unrecognized template type. Supported types:");
                foreach (var key in _templateGenerators.Keys) Console.WriteLine(key);
                return Task.FromResult(7);
            }

            (string? path, object? result) = action(options);

            if (File.Exists(path) && !options.Force)
            {
                Console.WriteLine(
                    $"A configuration file at {path} already exists. Use -f/--force to overwrite it.");
                return Task.FromResult(3);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new ApplicationException());
            using var tw = new StreamWriter(File.OpenWrite(path));
            ServerUtil.YamlSerializer.Serialize(tw, result);
            Console.WriteLine($"Template saved to:\n{path}");
            return Task.FromResult(0);
        }

        public async Task<int> Run(Executor<TDatabaseFactory> executor, IEnumerable<string> args) => await Parser
            .Default.ParseArguments<Options>(args)
            .MapResult(x => Start(executor, x), x => Task.FromResult(1)).Caf();
    }
}

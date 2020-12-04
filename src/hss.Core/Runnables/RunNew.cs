using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp;
using HacknetSharp.Server.Templates;

namespace hss.Core.Runnables
{
    [Verb("new", HelpText = "Create template.")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    internal class RunNew : Executor.IRunnable
    {
        private class Options
        {
            [Value(0, MetaName = "kind", HelpText = "Kind of template [system,person,world,server].", Required = true)]
            public string Kind { get; set; } = null!;

            [Option('n', "name", MetaValue = "name", HelpText = "Template name.")]
            public string? Name { get; set; }

            [Option('f', "force", HelpText = "Force overwrite existing template.")]
            public bool Force { get; set; }

            [Option('e', "example", HelpText = "Use example instead of empty.")]
            public bool Example { get; set; }
        }

        private static readonly
            Dictionary<string, (bool nameRequired, Func<Options, (string path, object result)> action)>
            _templateGenerators =
                new Dictionary<string, (bool nameRequired, Func<Options, (string path, object result)> action)>
                {
                    {
                        "system", (true, options => (
                            Path.Combine(ServerConstants.SystemTemplatesFolder, $"{options.Name}.yaml"),
                            (object)(options.Example
                                ? new SystemTemplate
                                {
                                    NameFormat = "{0}_HOMEBASE",
                                    OsName = "EncomOS",
                                    Users = new List<string>(new[] {"daphne:legacy", "samwise:genshin"}),
                                    Filesystem = new List<string>(new[]
                                    {
                                        "fold*+*:/bin", "fold:/etc", "fold:/home", "fold*+*:/lib", "fold:/mnt",
                                        "fold+++:/root", "fold:/usr", "fold:/usr/bin", "fold:/usr/lib",
                                        "fold:/usr/local", "fold:/usr/share", "fold:/var", "fold:/var/spool",
                                        "text:\"/home/samwise/read me.txt\" mr. frodo, sir!",
                                        "file:/home/samwise/image.png misc/image.png", "prog:/bin/cat core:cat",
                                        "prog:/bin/cd core:cd", "prog:/bin/ls core:ls", "prog:/bin/echo core:echo"
                                    })
                                }
                                : new SystemTemplate()))
                        )
                    },
                    {
                        "person", (true, options => (
                            Path.Combine(ServerConstants.PersonTemplatesFolder, $"{options.Name}.yaml"),
                            (object)(options.Example
                                ? new PersonTemplate
                                {
                                    Usernames = new List<string> {"locke", "bacon", "hayleyk653"},
                                    Passwords = new List<string> {"misterchef", "baconbacon", "isucklol"},
                                    EmailProviders =
                                        new List<string> {"hentaimail.net", "thisisnotaproblem.org", "fbiopenup.gov"},
                                    SystemTemplates = new List<string> {"systemTemplate2"}
                                }
                                : new PersonTemplate()))
                        )
                    },
                    {
                        "world", (true, options => (
                            Path.Combine(ServerConstants.WorldTemplatesFolder, $"{options.Name}.yaml"),
                            (object)(options.Example
                                ? new WorldTemplate
                                {
                                    Label = "Liyue kinda sux",
                                    PlayerSystemTemplate = "playerTemplate",
                                    StartupCommandLine = "echo \"Starting shell...\"",
                                    PlayerAddressRange = Constants.DefaultAddressRange,
                                    Generators = new List<WorldTemplate.Generator>
                                    {
                                        new WorldTemplate.Generator {Count = 3, PersonTemplate = "personTemplate2"}
                                    }
                                }
                                : new WorldTemplate()))
                        )
                    },
                    {
                        "server", (false, options => (
                            $"{options.Name ?? "server"}.yaml",
                            (object)(options.Example
                                ? new ServerYaml
                                {
                                    Host = "127.0.0.1",
                                    DatabaseKind = "sqlite",
                                    SqliteFile = "hakase",
                                    DefaultWorld = "main"
                                }
                                : new ServerYaml()))
                        )
                    }
                };

        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static Task<int> Start(Executor executor, Options options)
        {
            bool nameRequired;
            Func<Options, (string path, object result)> action;
            if (!_templateGenerators.TryGetValue(options.Kind.ToLowerInvariant(), out var res))
            {
                Console.WriteLine("Unrecognized template type. Supported types:");
                foreach (var key in _templateGenerators.Keys) Console.WriteLine(key);
                return Task.FromResult(7);
            }

            (nameRequired, action) = res;

            if (options.Name == null && nameRequired)
            {
                Console.WriteLine("A name is required");
                return Task.FromResult(66);
            }

            (string? path, object? result) = action(options);

            if (File.Exists(path) && !options.Force)
            {
                Console.WriteLine(
                    $"A configuration file at {path} already exists. Use -f/--force to overwrite it.");
                return Task.FromResult(3);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ??
                                      throw new ApplicationException());
            using var tw = new StreamWriter(File.OpenWrite(path));
            ServerUtil.YamlSerializer.Serialize(tw, result);
            Console.WriteLine($"Template saved to:\n{path}");
            return Task.FromResult(0);
        }

        public async Task<int> Run(Executor executor, IEnumerable<string> args) => await Parser
            .Default.ParseArguments<Options>(args)
            .MapResult(x => Start(executor, x), x => Task.FromResult(1)).Caf();
    }
}

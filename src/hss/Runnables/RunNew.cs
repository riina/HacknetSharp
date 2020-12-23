using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp;
using HacknetSharp.Server;
using HacknetSharp.Server.Templates;

namespace hss.Runnables
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
                new()
                {
                    {
                        "system", (true, options => (
                            Path.Combine(HssConstants.ContentFolder, $"{options.Name}.system.yaml"),
                            (object)(options.Example
                                ? new SystemTemplate
                                {
                                    Name = "{Owner.UserName}_HOMEBASE",
                                    OsName = "EncomOS",
                                    Users =
                                        new Dictionary<string, string> {{"daphne", "legacy"}, {"seiteki", "genshin"}},
                                    Filesystem = new Dictionary<string, List<string>>
                                    {
                                        {
                                            "{Owner.UserName}",
                                            new List<string>(new[]
                                            {
                                                "fold*+*:/bin", "fold:/etc", "fold:/home", "fold*+*:/lib", "fold:/mnt",
                                                "fold+++:/root", "fold:/usr", "fold:/usr/bin", "fold:/usr/lib",
                                                "fold:/usr/local", "fold:/usr/share", "fold:/var", "fold:/var/spool",
                                                "text:\"/home/seiteki/read me.txt\" mr. frodo, sir!",
                                                "file:/home/seiteki/image.png misc/image.png", "prog:/bin/cat core:cat",
                                                "prog:/bin/cd core:cd", "prog:/bin/ls core:ls",
                                                "prog:/bin/echo core:echo"
                                            })
                                        }
                                    }
                                }
                                : new SystemTemplate()))
                        )
                    },
                    {
                        "person", (true, options => (
                            Path.Combine(HssConstants.ContentFolder, $"{options.Name}.person.yaml"),
                            (object)(options.Example
                                ? new PersonTemplate
                                {
                                    Usernames =
                                        new Dictionary<string, float> {{"locke", 1}, {"bacon", 1}, {"hayleyk653", 1}},
                                    Passwords = new Dictionary<string, float>
                                    {
                                        {"misterchef", 1}, {"baconbacon", 1}, {"isucklol", 1}
                                    },
                                    EmailProviders =
                                        new Dictionary<string, float>
                                        {
                                            {"hentaimail.net", 1}, {"thisisnotaproblem.org", 1}, {"fbiopenup.gov", 1}
                                        },
                                    PrimaryTemplates = new Dictionary<string, float> {{"systemTemplate2", 1}}
                                }
                                : new PersonTemplate()))
                        )
                    },
                    {
                        "world", (true, options => (
                            Path.Combine(HssConstants.ContentFolder, $"{options.Name}.world.yaml"),
                            (object)(options.Example
                                ? new WorldTemplate
                                {
                                    Label = "Liyue kinda sux",
                                    PlayerSystemTemplate = "playerTemplate",
                                    StartupCommandLine = "echo \"Starting shell...\"",
                                    PlayerAddressRange = Constants.DefaultAddressRange,
                                    People = new List<WorldTemplate.PersonGroup>
                                    {
                                        new() {Count = 3, Template = "personTemplate2"}
                                    }
                                }
                                : new WorldTemplate()))
                        )
                    },
                    {
                        "mission", (true, options => (
                            Path.Combine(HssConstants.ContentFolder, $"{options.Name}.mission.yaml"),
                            (object)(options.Example
                                ? new MissionTemplate {Start = "log(\"this is a test\", 0)"}
                                : new WorldTemplate()))
                        )
                    },
                    {
                        "server", (false, options => (
                            $"{options.Name ?? "server"}.yaml",
                            (object)(options.Example
                                ? new ServerSettings
                                {
                                    Host = "127.0.0.1",
                                    Database = new Dictionary<string, string>
                                    {
                                        {"Kind", "sqlite"}, {"SqliteFile", "hakase.db"}
                                    },
                                    DefaultWorld = "main"
                                }
                                : new ServerSettings()))
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
            .MapResult(x => Start(executor, x), _ => Task.FromResult(1)).Caf();
    }
}

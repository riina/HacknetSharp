using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp.Server.Common.Models;
using Microsoft.EntityFrameworkCore;
using YamlDotNet.Serialization;

namespace HacknetSharp.Server
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    public class Executor<TDatabaseFactory> : Executor where TDatabaseFactory : StorageContextFactoryBase
    {
        public HashSet<Type[]> CustomPrograms { get; set; } = _customPrograms;

        public async Task<int> Run(IEnumerable<string> args) =>
            await Parser.Default
                .ParseArguments<CertOptions, UserOptions, TokenOptions,
                    NewOptions, RunOptions
                >(args)
                .MapResult<CertOptions, UserOptions, TokenOptions,
                    NewOptions, RunOptions, Task<int>>(
                    RunCert,
                    RunUser,
                    RunToken,
                    RunNew,
                    RunRun,
                    errs => Task.FromResult(1)).Caf();

        private class OptionsBase
        {
            [Option('c', "create", HelpText = "Create entries.", SetName = "create")]
            public bool Create { get; set; }

            [Option('r', "remove", HelpText = "Remove entries.", SetName = "remove")]
            public bool Remove { get; set; }
        }

        // TODO purge verb (clear database of content outside specified worlds)

        [Verb("cert", HelpText = "Manage server certificate.")]
        private class CertOptions
        {
            [Option('s', "search", HelpText = "Search for existing PKCS#12 X509 cert.", MetaValue = "externalAddr",
                SetName = "create")]
            public string? Search { get; set; }

            [Option('r', "register", HelpText = "Register PKCS#12 X509 key/cert.", MetaValue = "certFile",
                SetName = "create")]
            public string? Register { get; set; }

            [Option('d', "deregister", HelpText = "Deregister PKCS#12 X509 key/cert.", MetaValue = "certFile",
                SetName = "remove")]
            public string? Deregister { get; set; }
        }

        private static Task<int> RunCert(CertOptions options)
        {
            if (options.Register != null)
            {
                X509Certificate2? nCert = null;
                try
                {
                    var ss = Util.PromptSecureString("Pfx/p12 export password:");
                    if (ss == null)
                        return Task.FromResult(0);
                    try
                    {
                        nCert = new X509Certificate2(options.Register, ss);
                    }
                    finally
                    {
                        ss.Dispose();
                    }

                    foreach ((StoreName name, StoreLocation location) in _wStores)
                    {
                        Console.WriteLine($"Registering to {location}:{name}...");
                        var nStore = new X509Store(name, location);
                        nStore.Open(OpenFlags.ReadWrite);
                        nStore.Add(nCert);
                        nStore.Close();
                    }
                }
                finally
                {
                    nCert?.Dispose();
                }

                Console.WriteLine("Cert registration complete.");
                return Task.FromResult(0);
            }

            if (options.Deregister != null)
            {
                X509Certificate2? nCert = null;
                try
                {
                    var ss = Util.PromptSecureString("Pfx/p12 export password:");
                    if (ss == null)
                        return Task.FromResult(0);
                    try
                    {
                        nCert = new X509Certificate2(options.Deregister, ss);
                    }
                    finally
                    {
                        ss.Dispose();
                    }

                    foreach ((StoreName name, StoreLocation location) in _wStores)
                    {
                        Console.WriteLine($"Removing from {location}:{name}...");
                        var nStore = new X509Store(name, location);
                        nStore.Open(OpenFlags.ReadWrite);
                        nStore.Remove(nCert);
                        nStore.Close();
                    }
                }
                finally
                {
                    nCert?.Dispose();
                }

                Console.WriteLine("Cert removal complete.");
                return Task.FromResult(0);
            }

            if (options.Search != null)
            {
                Console.WriteLine("Looking for cert...");
                var cert = FindCertificate(options.Search);
                if (cert == null)
                {
                    Console.WriteLine("Failed to find certificate");
                    return Task.FromResult(304);
                }

                Console.WriteLine(
                    $"Found cert in {cert.Value.Item1.Location}:{cert.Value.Item1.Location} - {cert.Value.Item2.Subject}");
                return Task.FromResult(0);
            }

            return Task.FromResult(0);
        }

        [Verb("token", HelpText = "Manage tokens.")]
        private class TokenOptions
        {
            [Value(0, MetaName = "names", HelpText = "Forger names.")]
            public IEnumerable<string> Names { get; set; } = null!;

            [Option('r', "remove", HelpText = "Remove entries.", SetName = "remove")]
            public bool Remove { get; set; }

            [Option('a', "all", HelpText = "Operate on all entries.")]
            public bool All { get; set; }
        }

        private static async Task<int> RunToken(TokenOptions options)
        {
            var factory = Activator.CreateInstance<TDatabaseFactory>();
            await using var ctx = factory.CreateDbContext(Array.Empty<string>());
            var names = new HashSet<string>(options.Names);
            var tokens = await (options.All
                ? ctx.Set<RegistrationToken>()
                : ctx.Set<RegistrationToken>().Where(u => names.Contains(u.Forger.Key))).ToListAsync().Caf();

            if (options.Remove)
            {
                ctx.RemoveRange(tokens);
                await ctx.SaveChangesAsync().Caf();
                return 0;
            }

            foreach (var token in tokens)
                Console.WriteLine($"{token.Forger.Key}");

            return 0;
        }

        [Verb("user", HelpText = "Manage users.")]
        private class UserOptions : OptionsBase
        {
            [Option("admin", HelpText = "Make user admin.", SetName = "create")]
            public bool Admin { get; set; }

            [Value(0, MetaName = "names", HelpText = "Entry names.")]
            public IEnumerable<string> Names { get; set; } = null!;

            [Option('a', "all", HelpText = "Operate on all entries.")]
            public bool All { get; set; }
        }

        private static async Task<int> RunUser(UserOptions options)
        {
            var factory = Activator.CreateInstance<TDatabaseFactory>();
            await using var ctx = factory.CreateDbContext(Array.Empty<string>());
            var names = new HashSet<string>(options.Names);
            if (options.Create)
            {
                if (names.Count != 1)
                {
                    Console.WriteLine("Only 1 user may be specified.");
                    return 77;
                }

                string name = names.First();
                var xizt = await ctx.FindAsync<UserModel>(name).Caf();
                if (xizt != null)
                {
                    Console.WriteLine("A user with specified name already exists.");
                    return 0;
                }

                string? pass = Util.PromptPassword("Pass:");
                if (pass == null) return 0;
                var (salt, hash) = AccessController.Base64Password(pass);
                ctx.Add(new UserModel {Admin = options.Admin, Base64Password = hash, Base64Salt = salt, Key = name});
                await ctx.SaveChangesAsync().Caf();
                return 0;
            }

            var users = await (options.All
                ? ctx.Set<UserModel>()
                : ctx.Set<UserModel>().Where(u => names.Contains(u.Key))).ToListAsync().Caf();

            if (options.Remove)
            {
                foreach (var user in users)
                {
                    var player = await ctx.FindAsync<PlayerModel>(user.Key).Caf();
                    if (player != null)
                    {
                        foreach (var person in player.Identities)
                        {
                            foreach (var system in person.Systems) ctx.RemoveRange(system.Files);
                            ctx.RemoveRange(person.Systems);
                        }

                        ctx.RemoveRange(player.Identities);
                        ctx.Remove(player);
                    }
                }

                ctx.RemoveRange(users);
                await ctx.SaveChangesAsync().Caf();
                return 0;
            }

            foreach (var user in users)
                Console.WriteLine($"{user.Key}:{(user.Admin ? "admin" : "regular")}");

            return 0;
        }

        [Verb("new", HelpText = "Create template.")]
        private class NewOptions
        {
            [Value(0, MetaName = "kind", HelpText = "Kind of template.", Required = true)]
            public string Kind { get; set; } = null!;

            [Value(1, MetaName = "name", HelpText = "Name for template instance.", Required = true)]
            public string Name { get; set; } = null!;

            [Option('f', "force", HelpText = "Force overwrite existing template.")]
            public bool Force { get; set; }
        }

        private static readonly Dictionary<string, Func<NewOptions, (string path, object result)>> _templateGenerators =
            new Dictionary<string, Func<NewOptions, (string path, object result)>>
            {
                {
                    "system", options => (Path.Combine(ServerConstants.SystemTemplatesFolder, $"{options.Name}.yaml"),
                        (object)new SystemTemplate
                        {
                            OsName = "EncomOS",
                            Users = new List<string>(new[] {"{u}+:{p}", "samwise:genshin"}),
                            Filesystem = new List<string>(new[]
                            {
                                "fold*+*:/bin", "fold:/etc", "fold:/home", "fold*+*:/lib", "fold:/mnt",
                                "fold+++:/root", "fold:/usr", "fold:/usr/bin", "fold:/usr/lib", "fold:/usr/local",
                                "fold:/usr/share", "fold:/var", "fold:/var/spool",
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

        private static Task<int> RunNew(NewOptions options)
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
            _serializer.Serialize(tw, result);
            Console.WriteLine($"Template saved to:\n{path}");
            return Task.FromResult(0);
        }

        [Verb("run", HelpText = "Run server.")]
        private class RunOptions
        {
            [Value(0, MetaName = "externalAddr", HelpText = "External address.", Required = true)]
            public string ExternalAddr { get; set; } = null!;

            [Value(1, MetaName = "defaultWorld", HelpText = "Default world to load.", Required = true)]
            public string DefaultWorld { get; set; } = null!;
        }

        private static (X509Store, X509Certificate2)? FindCertificate(string externalAddr)
        {
            foreach ((StoreName name, StoreLocation location) in _wStores)
            {
                var store = new X509Store(name, location);
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindBySubjectName, externalAddr, false);
                store.Close();
                if (certs.Count <= 0) continue;
                return (store, certs[0]);
            }

            return null;
        }

        private async Task<int> RunRun(RunOptions options)
        {
            Console.WriteLine("Looking for cert...");
            var cert = FindCertificate(options.ExternalAddr);
            if (cert == null)
            {
                Console.WriteLine("Failed to find certificate");
                return 304;
            }

            Console.WriteLine(
                $"Found cert in {cert.Value.Item1.Location}:{cert.Value.Item1.Location} - {cert.Value.Item2.Subject}");


            var conf = new ServerConfig()
                .WithPrograms(CustomPrograms)
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
            _deserializer.Deserialize<T>(File.ReadAllText(file));
    }

    public class Executor
    {
        internal static readonly (StoreName name, StoreLocation location)[] _wStores =
        {
            (StoreName.Root, StoreLocation.CurrentUser), (StoreName.My, StoreLocation.CurrentUser)
        };

        protected static readonly HashSet<Type[]> _customPrograms =
            ServerUtil.LoadProgramTypesFromFolder(ServerConstants.ExtensionsFolder);

        protected static readonly IDeserializer _deserializer = new DeserializerBuilder().Build();
        protected static readonly ISerializer _serializer = new SerializerBuilder().Build();
    }
}

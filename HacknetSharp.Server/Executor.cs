using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CommandLine;
using YamlDotNet.Serialization;

namespace HacknetSharp.Server
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    public class Executor<TDatabaseFactory> : Executor where TDatabaseFactory : StorageContextFactoryBase
    {
        public HashSet<Type[]> CustomPrograms { get; set; } = new HashSet<Type[]>();

        public async Task<int> Run(string[] args) =>
            await Parser.Default
                .ParseArguments<RegisterAdminOptions, DeregisterOptions, InstallCertOptions, UninstallCertOptions,
                    TemplateOptions, RunOptions
                >(args)
                .MapResult<RegisterAdminOptions, DeregisterOptions, InstallCertOptions, UninstallCertOptions,
                    TemplateOptions, RunOptions, Task<int>>(
                    RunRegisterAdmin,
                    RunDeregisterAdmin,
                    RunInstallCert,
                    RunUninstallCert,
                    RunTemplate,
                    RunRun,
                    errs => Task.FromResult(1));

        // TODO purge verb (clear database of content outside specified worlds)

        [Verb("installcert", HelpText = "install server certificate")]
        private class InstallCertOptions
        {
            [Value(0, MetaName = "certFile", HelpText = "File with PKCS#12 X509 key/cert", Required = true)]
            public string CertFile { get; set; } = null!;
        }

        private Task<int> RunInstallCert(InstallCertOptions options)
        {
            X509Certificate2? nCert = null;
            try
            {
                var ss = PromptSecureString("Pfx/p12 export password:");
                if (ss == null)
                    return Task.FromResult(0);
                try
                {
                    nCert = new X509Certificate2(options.CertFile, ss);
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

        [Verb("uninstallcert", HelpText = "uninstall server certificate")]
        private class UninstallCertOptions
        {
            [Value(0, MetaName = "certFile", HelpText = "File with PKCS#12 X509 key/cert", Required = true)]
            public string CertFile { get; set; } = null!;
        }

        private Task<int> RunUninstallCert(UninstallCertOptions options)
        {
            X509Certificate2? nCert = null;
            try
            {
                var ss = PromptSecureString("Pfx/p12 export password:");
                if (ss == null)
                    return Task.FromResult(0);
                try
                {
                    nCert = new X509Certificate2(options.CertFile, ss);
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

        [Verb("registeradmin", HelpText = "register admin user")]
        private class RegisterAdminOptions
        {
            [Value(0, MetaName = "name", HelpText = "User name", Required = true)]
            public string Name { get; set; } = null!;
        }

        private async Task<int> RunRegisterAdmin(RegisterAdminOptions options)
        {
            var factory = Activator.CreateInstance<TDatabaseFactory>();
            var ctx = factory.CreateDbContext(Array.Empty<string>());
            var xizt = await ctx.FindAsync<UserModel>(options.Name);
            if (xizt != null)
            {
                Console.WriteLine("A user with specified name already exists.");
                return 0;
            }

            string? pass = Util.PromptPassword("Pass:");
            if (pass == null) return 0;
            var (salt, hash) = AccessController.Base64Password(pass);
            ctx.Add(new UserModel {Admin = true, Base64Password = hash, Base64Salt = salt, Key = options.Name});
            await ctx.SaveChangesAsync();
            return 0;
        }

        [Verb("deregister", HelpText = "deregister user")]
        private class DeregisterOptions
        {
            [Value(0, MetaName = "name", HelpText = "User name", Required = true)]
            public string Name { get; set; } = null!;
        }

        private async Task<int> RunDeregisterAdmin(DeregisterOptions options)
        {
            var factory = Activator.CreateInstance<TDatabaseFactory>();
            var ctx = factory.CreateDbContext(new string[0]);
            var user = ctx.Find<UserModel>(options.Name);
            if (user == null)
            {
                Console.WriteLine("Could not find user with specified name.");
                return 0;
            }

            ctx.Remove(user);
            await ctx.SaveChangesAsync();
            return 0;
        }

        [Verb("template", HelpText = "create templates")]
        private class TemplateOptions
        {
            [Value(0, MetaName = "kind", HelpText = "kind of template", Required = true)]
            public string Kind { get; set; } = null!;

            [Value(1, MetaName = "name", HelpText = "name for template instance", Required = true)]
            public string Name { get; set; } = null!;

            [Option('f', "force", HelpText = "Force overwrite existing template.")]
            public bool Force { get; set; }
        }

        private Task<int> RunTemplate(TemplateOptions options)
        {
            var (path, result) =
                options.Kind.ToLowerInvariant() switch
                {
                    "system" =>
                        (Path.Combine(Constants.SystemTemplatesFolder, $"{options.Name}.yaml"),
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
                            }),
                    "world" => (
                        Path.Combine(Constants.WorldTemplatesFolder, $"{options.Name}.yaml"),
                        (object)new WorldTemplate {Name = options.Name}),
                    _ => (null, null)
                };

            if (path == null || result == null)
            {
                Console.WriteLine("Unrecognized template type.");
                return Task.FromResult(7);
            }

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

        [Verb("run", HelpText = "run server")]
        private class RunOptions
        {
            [Value(0, MetaName = "externalAddr", HelpText = "External address.", Required = true)]
            public string ExternalAddr { get; set; } = null!;

            [Value(1, MetaName = "defaultWorld", HelpText = "Default world to load.", Required = true)]
            public string DefaultWorld { get; set; } = null!;
        }

        private async Task<int> RunRun(RunOptions options)
        {
            Console.WriteLine("Looking for cert...");
            X509Certificate? cert = null;
            foreach ((StoreName name, StoreLocation location) in _wStores)
            {
                var store = new X509Store(name, location);
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindBySubjectName, options.ExternalAddr, false);
                store.Close();
                if (certs.Count <= 0) continue;
                cert = certs[0];
                Console.WriteLine($"Found cert in {location}:{name} - {cert.Subject}");
                break;
            }

            if (cert == null)
            {
                Console.WriteLine("Failed to find certificate");
                return 304;
            }


            var conf = new ServerConfig()
                .WithPrograms(CustomPrograms)
                .WithStorageContextFactory<TDatabaseFactory>()
                .WithAccessController<AccessController>()
                .WithDefaultWorld(options.DefaultWorld)
                .WithPort(42069)
                .WithCertificate(cert);
            if (Directory.Exists(Constants.WorldTemplatesFolder))
                conf.WithWorldTemplates(Directory.EnumerateFiles(Constants.WorldTemplatesFolder)
                    .Select(ReadFromFile<WorldTemplate>));
            if (Directory.Exists(Constants.SystemTemplatesFolder))
                conf.WithSystemTemplates(Directory.EnumerateFiles(Constants.SystemTemplatesFolder)
                    .Select(ReadFromFile<SystemTemplate>));
            var instance = conf.CreateInstance();
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

        private static T ReadFromFile<T>(string file) =>
            _deserializer.Deserialize<T>(File.ReadAllText(file));

        /// <summary>
        /// Prompt user for SecureString password
        /// </summary>
        /// <param name="mes">Prompt message</param>
        /// <returns>Password or null if terminated</returns>
        public static SecureString? PromptSecureString(string mes)
        {
            Console.Write(mes);

            var ss = new SecureString();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.C && (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control)
                {
                    ss.Dispose();
                    return null;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (ss.Length != 0)
                        ss.RemoveAt(ss.Length - 1);
                    continue;
                }

                ss.AppendChar(key.KeyChar);
            }

            Console.WriteLine();
            return ss;
        }
    }

    public class Executor
    {
        internal static readonly (StoreName name, StoreLocation location)[] _wStores =
        {
            /*(StoreName.My, StoreLocation.CurrentUser), */(StoreName.Root, StoreLocation.CurrentUser)
        };

        protected static readonly IDeserializer _deserializer = new DeserializerBuilder().Build();
        protected static readonly ISerializer _serializer = new SerializerBuilder().Build();
    }
}

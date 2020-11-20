using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp.Server.Sqlite;
using YamlDotNet.Serialization;

namespace HacknetSharp.Server.Standard
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
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

        private class StandardSqliteStorageContextFactory : SqliteStorageContextFactory
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
                .ParseArguments<InstallCertOptions, UninstallCertOptions, CreateOptions, RunOptions
                >(args)
                .MapResult<InstallCertOptions, UninstallCertOptions, CreateOptions, RunOptions, Task<int>>(
                    RunInstallCert,
                    RunUninstallCert,
                    RunCreate,
                    RunRun,
                    errs => Task.FromResult(1));

        // TODO purge verb (clear database of content outside specified worlds)

        private static readonly (StoreName name, StoreLocation location)[] _wStores =
        {
            (StoreName.My, StoreLocation.CurrentUser), (StoreName.Root, StoreLocation.CurrentUser),
        };

        [Verb("installcert", HelpText = "install server certificate")]
        private class InstallCertOptions
        {
            [Value(0, MetaName = "certFile", HelpText = "File with PKCS#12 X509 key/cert", Required = true)]
            public string CertFile { get; set; } = null!;
        }

        private static Task<int> RunInstallCert(InstallCertOptions options)
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

        private static Task<int> RunUninstallCert(UninstallCertOptions options)
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

        [Verb("create", HelpText = "create world configuration")]
        private class CreateOptions
        {
            [Value(0, MetaName = "worldName", HelpText = "Name of world", Required = true)]
            public string WorldName { get; set; } = null!;

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
            [Value(0, MetaName = "externalAddr", HelpText = "External address.", Required = true)]
            public string ExternalAddr { get; set; } = null!;

            [Value(1, MetaName = "worldConfigs", HelpText = "World configuration YAML files.")]
            public IEnumerable<string> WorldConfigs { get; set; } = null!;
        }

        private static async Task<int> RunRun(RunOptions options)
        {
            // TODO maybe programs -> types (declare with attrs)
            Console.WriteLine("Looking for cert...");
            X509Certificate? cert = null;
            foreach ((StoreName name, StoreLocation location) in _wStores)
            {
                var store = new X509Store(name, location);
                store.Open(OpenFlags.ReadOnly);
                var certs = store.Certificates.Find(X509FindType.FindBySubjectName, options.ExternalAddr, false);
                store.Close();
                if (certs.Count <= 0) continue;
                Console.WriteLine($"Found cert in {location}:{name}");
                cert = certs[0];
                break;
            }

            if (cert == null)
            {
                Console.WriteLine("Failed to find certificate");
                return 304;
            }


            var instance = new ServerConfig()
                .WithModels(_models)
                .WithPrograms(_programs)
                .WithStorageContextFactory<StandardSqliteStorageContextFactory>()
                .WithAccessController<StandardAccessController>()
                .WithWorldConfigs(options.WorldConfigs.Select(ReadWorldConfigFromFile))
                .WithPort(42069)
                .WithCertificate(cert)
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
}

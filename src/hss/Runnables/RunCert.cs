using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp;

namespace hss.Runnables
{
    [Verb("cert", HelpText = "Manage server certificate.")]
    internal class RunCert : Executor.IRunnable
    {
        [Verb("search", HelpText = "Search for existing PKCS#12 X509 cert.")]
        private class Search : Executor.ISelfRunnable
        {
            [Value(0, HelpText = "External address to search for.", MetaName = "externalAddr", Required = true)]
            public string ExternalAddr { get; set; } = null!;


            public Task<int> Run(Executor executor)
            {
                Console.WriteLine("Looking for cert...");
                var cert = ServerUtil.FindCertificate(ExternalAddr, ServerUtil.CertificateStores);
                if (cert == null)
                {
                    Console.WriteLine("Failed to find certificate");
                    return Task.FromResult(304);
                }

                Console.WriteLine(
                    $"Found cert in {cert.Value.Item1.Location}:{cert.Value.Item1.Location} - {cert.Value.Item2.Subject}");
                return Task.FromResult(0);
            }
        }

        [Verb("install", HelpText = "Install PKCS#12 X509 cert.")]
        private class Register : Executor.ISelfRunnable
        {
            [Value(0, HelpText = "PKCS#12 X509 certificate to use.", MetaName = "cert", Required = true)]
            public string Certificate { get; set; } = null!;

            public Task<int> Run(Executor executor)
            {
                X509Certificate2? nCert = null;
                try
                {
                    Console.Write("Pfx/p12 export password:");
                    var ss = Util.ReadSecureString();
                    if (ss == null)
                        return Task.FromResult(0);
                    try
                    {
                        nCert = new X509Certificate2(Certificate, ss);
                    }
                    finally
                    {
                        ss.Dispose();
                    }

                    foreach ((StoreName name, StoreLocation location) in ServerUtil.CertificateStores)
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
        }

        [Verb("remove", HelpText = "Remove existing PKCS#12 X509 cert.")]
        private class Deregister : Executor.ISelfRunnable
        {
            [Value(0, HelpText = "PKCS#12 X509 certificate to use.", MetaName = "cert", Required = true)]
            public string Certificate { get; set; } = null!;

            public Task<int> Run(Executor executor)
            {
                X509Certificate2? nCert = null;
                try
                {
                    Console.Write("Pfx/p12 export password:");
                    var ss = Util.ReadSecureString();
                    if (ss == null)
                        return Task.FromResult(0);
                    try
                    {
                        nCert = new X509Certificate2(Certificate, ss);
                    }
                    finally
                    {
                        ss.Dispose();
                    }

                    foreach ((StoreName name, StoreLocation location) in ServerUtil.CertificateStores)
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
        }

        public async Task<int> Run(Executor executor, IEnumerable<string> args) =>
            await Parser.Default.ParseArguments<Search, Register, Deregister>(args)
                .MapResult<Executor.ISelfRunnable, Task<int>>(x => x.Run(executor),
                    x => Task.FromResult(1)).Caf();
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using HacknetSharp.Server;
using YamlDotNet.Serialization;

namespace hss
{
    /// <summary>
    /// Contains utility methods.
    /// </summary>
    public static class ServerUtil
    {
        public static (X509Store, X509Certificate2)? FindCertificate(string externalAddr,
            IEnumerable<(StoreName name, StoreLocation location)> stores)
        {
            foreach ((StoreName name, StoreLocation location) in stores)
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

        internal static readonly (StoreName name, StoreLocation location)[] CertificateStores =
        {
            (StoreName.Root, StoreLocation.CurrentUser), (StoreName.My, StoreLocation.CurrentUser)
        };


        internal static readonly IDeserializer YamlDeserializer = new DeserializerBuilder().Build();
        internal static readonly ISerializer YamlSerializer = new SerializerBuilder().Build();


        public static TemplateGroup GetTemplates(string installDir)
        {
            var templates = new TemplateGroup();
            string worlds = Path.Combine(installDir, ServerConstants.WorldTemplatesFolder);
            if (Directory.Exists(worlds))
                LoadTree(worlds, templates.WorldTemplates, ".yaml");

            string persons = Path.Combine(installDir, ServerConstants.PersonTemplatesFolder);
            if (Directory.Exists(persons))
                LoadTree(persons, templates.PersonTemplates, ".yaml");

            string systems = Path.Combine(installDir, ServerConstants.SystemTemplatesFolder);
            if (Directory.Exists(systems))
                LoadTree(systems, templates.SystemTemplates, ".yaml");

            return templates;
        }

        private static void LoadTree<TValue>(string root, Dictionary<string, TValue> dict, string? extension)
        {
            Queue<string> dQueue = new Queue<string>();
            Queue<string> fQueue = new Queue<string>();
            dQueue.Enqueue("");
            while (fQueue.Count != 0 || dQueue.Count != 0)
            {
                if (fQueue.Count != 0)
                {
                    string file = fQueue.Dequeue();
                    dict.Add(file, ReadFromFile<TValue>(Path.Combine(root, file)).Item2);
                }
                else
                {
                    string curDir = dQueue.Dequeue();
                    string absDir = Path.Combine(root, curDir);
                    if (!Directory.Exists(absDir)) continue;
                    foreach (string file in Directory.EnumerateFiles(absDir).Where(f =>
                        extension == null || string.Equals(extension, Path.GetExtension(f),
                            StringComparison.InvariantCultureIgnoreCase)))
                        fQueue.Enqueue(Program.Combine(curDir, Path.GetFileName(file)));
                    foreach (string folder in Directory.EnumerateDirectories(absDir))
                        dQueue.Enqueue(Program.Combine(curDir, Path.GetFileName(folder)));
                }
            }
        }

        public static (string, T) ReadFromFile<T>(string file) => (
            Path.GetFileNameWithoutExtension(file).ToLowerInvariant(),
            YamlDeserializer.Deserialize<T>(File.ReadAllText(file)));
    }
}

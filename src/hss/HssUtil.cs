using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using HacknetSharp.Server;
using HacknetSharp.Server.Templates;
using YamlDotNet.Serialization;

namespace hss
{
    /// <summary>
    /// Contains utility methods.
    /// </summary>
    public static class HssUtil
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

        public static void LoadTemplates(TemplateGroup templates, ServerSettings settings, string dir)
        {
            LoadTemplates(templates, Path.Combine(dir, HssConstants.ContentFolder));
            if (settings.ContentFolders == null) return;
            foreach (var path in settings.ContentFolders)
                LoadTemplates(templates, Path.Combine(dir, path));
        }

        public static void LoadTemplates(TemplateGroup templates, string dir)
        {
            Dictionary<string, Action<string, string>> templateLoadDict = new()
            {
                {
                    ".world.yaml",
                    (file, path) => templates.WorldTemplates.Add(file, ReadFromFile<WorldTemplate>(path).Item2)
                },
                {
                    ".person.yaml",
                    (file, path) => templates.PersonTemplates.Add(file, ReadFromFile<PersonTemplate>(path).Item2)
                },
                {
                    ".system.yaml",
                    (file, path) => templates.SystemTemplates.Add(file, ReadFromFile<SystemTemplate>(path).Item2)
                },
            };
            LoadTree(dir, templateLoadDict);
        }

        private static void LoadTree(string root, Dictionary<string, Action<string, string>> actionDict)
        {
            Queue<string> dQueue = new();
            Queue<string> fQueue = new();
            dQueue.Enqueue("");
            while (fQueue.Count != 0 || dQueue.Count != 0)
            {
                if (fQueue.Count != 0)
                {
                    string file = fQueue.Dequeue();
                    string? str = actionDict.Keys.FirstOrDefault(k =>
                        file.EndsWith(k, StringComparison.InvariantCultureIgnoreCase));
                    if (str != null) actionDict[str](file, Path.Combine(root, file));
                }
                else
                {
                    string curDir = dQueue.Dequeue();
                    string absDir = Path.Combine(root, curDir);
                    if (!Directory.Exists(absDir)) continue;
                    foreach (string file in Directory.EnumerateFiles(absDir))
                        fQueue.Enqueue(Executable.Combine(curDir, Path.GetFileName(file)));
                    foreach (string folder in Directory.EnumerateDirectories(absDir))
                        dQueue.Enqueue(Executable.Combine(curDir, Path.GetFileName(folder)));
                }
            }
        }

        public static (string, T) ReadFromFile<T>(string file) => (
            Path.GetFileNameWithoutExtension(file).ToLowerInvariant(),
            ServerUtil.YamlDeserializer.Deserialize<T>(File.ReadAllText(file)));
    }
}

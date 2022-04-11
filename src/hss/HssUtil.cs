using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using HacknetSharp.Server;
using HacknetSharp.Server.Templates;

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

        internal static readonly (StoreName name, StoreLocation location)[] CertificateStores = { (StoreName.Root, StoreLocation.CurrentUser), (StoreName.My, StoreLocation.CurrentUser) };

        public static void LoadTemplates(TemplateGroup templates, IEnumerable<string>? contentFolders, string dir)
        {
            LoadTemplates(templates, Path.Combine(dir, HssConstants.ContentFolder));
            if (contentFolders == null) return;
            foreach (var path in contentFolders)
                LoadTemplates(templates, Path.Combine(dir, path));
        }

        public static void LoadTemplates(TemplateGroup templates, string dir)
        {
            Dictionary<string, Action<string, string>> templateLoadDict = new()
            {
                { ".world.yaml", (file, path) => templates.WorldTemplates.Add(file, DefaultContentImporterGroup.ImportNotNull<WorldTemplate>(path)) },
                { ".person.yaml", (file, path) => templates.PersonTemplates.Add(file, DefaultContentImporterGroup.ImportNotNull<PersonTemplate>(path)) },
                { ".system.yaml", (file, path) => templates.SystemTemplates.Add(file, DefaultContentImporterGroup.ImportNotNull<SystemTemplate>(path)) },
                { ".mission.yaml", (file, path) => templates.MissionTemplates.Add(file, DefaultContentImporterGroup.ImportNotNull<MissionTemplate>(path)) },
                { ".world.lua", (file, path) => templates.WorldTemplates.Add(file, DefaultContentImporterGroup.ImportNotNull<WorldTemplate>(path)) },
                { ".person.lua", (file, path) => templates.PersonTemplates.Add(file, DefaultContentImporterGroup.ImportNotNull<PersonTemplate>(path)) },
                { ".system.lua", (file, path) => templates.SystemTemplates.Add(file, DefaultContentImporterGroup.ImportNotNull<SystemTemplate>(path)) },
                { ".mission.lua", (file, path) => templates.MissionTemplates.Add(file, DefaultContentImporterGroup.ImportNotNull<MissionTemplate>(path)) },
                { ".script.lua", (file, path) => templates.LuaSources.Add(file, () => File.OpenRead(path)) }
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

        private static readonly YamlContentImporter s_yamlContentImporter = new();
        private static readonly LuaContentImporter s_luaContentImporter = new();

        public static ContentImporterGroup DefaultContentImporterGroup = new((".yml", s_yamlContentImporter), (".yaml", s_yamlContentImporter), (".lua", s_luaContentImporter));
    }
}

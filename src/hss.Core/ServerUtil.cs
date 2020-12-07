using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using HacknetSharp;
using HacknetSharp.Server;
using YamlDotNet.Serialization;

namespace hss.Core
{
    /// <summary>
    /// Contains utility methods.
    /// </summary>
    public static class ServerUtil
    {
        public static IEnumerable<Type> GetTypes(Type t, Assembly assembly) =>
            assembly.GetTypes().Where(type => IsSubclass(t, type) && !type.IsAbstract);

        internal static readonly HashSet<Type> DefaultModels =
            new HashSet<Type>(GetTypes(typeof(Model<>), typeof(Model<>).Assembly)
                .Concat(GetTypes(typeof(Model<>), typeof(ServerUtil).Assembly)));

        internal static readonly HashSet<Type> DefaultPrograms =
            new HashSet<Type>(GetTypes(typeof(Program), typeof(Program).Assembly)
                .Concat(GetTypes(typeof(Program), typeof(ServerUtil).Assembly)));

        internal static readonly HashSet<Type> DefaultServices =
            new HashSet<Type>(GetTypes(typeof(Service), typeof(Service).Assembly)
                .Concat(GetTypes(typeof(Service), typeof(ServerUtil).Assembly)));

        /// <summary>
        /// Load types from a folder
        /// </summary>
        /// <param name="folder">Search folder</param>
        /// <param name="types">Types to search</param>
        /// <returns>List of lists of types</returns>
        public static List<List<Type>> LoadTypesFromFolder(string folder, IReadOnlyList<Type> types)
        {
            List<List<Type>> ret = new List<List<Type>>();
            for (int i = 0; i < types.Count; i++)
                ret.Add(new List<Type>());
            if (!Directory.Exists(folder)) return ret;
            var opts = new EnumerationOptions {MatchCasing = MatchCasing.CaseInsensitive};
            try
            {
                foreach (string d in Directory.GetDirectories(folder))
                {
                    try
                    {
                        string tarName = Path.GetFileName(d);
                        string? fDll = Directory.GetFiles(d, $"{tarName}.dll", opts).FirstOrDefault();
                        if (fDll == null) continue;
                        var assembly = Assembly.LoadFrom(fDll);
                        for (int i = 0; i < types.Count; i++)
                            ret[i].AddRange(GetTypes(types[i], assembly));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return ret;
        }

        public static bool IsSubclass(Type @base, Type? toCheck) =>
            @base != toCheck && (@base.IsGenericType
                ? IsSubclassOfRawGeneric(@base, toCheck)
                : toCheck?.IsAssignableTo(@base) ?? false);

        // https://stackoverflow.com/a/457708
        private static bool IsSubclassOfRawGeneric(Type generic, Type? toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }

                toCheck = toCheck.BaseType;
            }

            return false;
        }

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

        public static bool TryParseConString(string conString, ushort defaultPort, [NotNullWhen(true)] out string? name,
            [NotNullWhen(true)] out string? host, out ushort port, [NotNullWhen(false)] out string? error) =>
            Util.TryParseConString(conString, defaultPort, out name!, out host!, out port, out error!);

        public static bool ValidatePassword(string pass, byte[] hash, byte[] salt)
        {
            var (genHash, _) = CommonUtil.HashPassword(pass, salt: salt);
            return genHash.AsSpan().SequenceEqual(hash);
        }
    }
}

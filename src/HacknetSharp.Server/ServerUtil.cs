using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using HacknetSharp.Server.Common;
using HacknetSharp.Server.Common.Models;
using YamlDotNet.Serialization;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Contains utility methods.
    /// </summary>
    public static class ServerUtil
    {
        public static void Consideration(Action<FileModel?> files, Action<SystemModel?> systems,
            Action<PersonModel?> persons, Action<PlayerModel?> players, Action<WorldModel?> worlds)
        {
            files(null);
            systems(null);
            persons(null);
            players(null);
            worlds(null);
        }

        public static IEnumerable<Type> GetTypes(Type t, Assembly assembly) =>
            assembly.GetTypes().Where(type => IsSubclass(t, type) && !type.IsAbstract);

        internal static readonly HashSet<Type> DefaultModels =
            new HashSet<Type>(GetTypes(typeof(Model<>), typeof(Model<>).Assembly)
                .Concat(GetTypes(typeof(Model<>), typeof(ServerUtil).Assembly)));


        internal static readonly HashSet<Type> DefaultPrograms =
            new HashSet<Type>(GetTypes(typeof(Program), typeof(Program).Assembly)
                .Concat(GetTypes(typeof(Program), typeof(ServerUtil).Assembly)));

        /// <summary>
        /// Load program types from a folder
        /// </summary>
        /// <param name="folder">Search folder</param>
        /// <returns>Set of type arrays</returns>
        public static HashSet<Type[]> LoadProgramTypesFromFolder(string folder)
        {
            HashSet<Type[]> ret = new HashSet<Type[]>();
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
                        if (fDll != null)
                        {
                            var assembly = Assembly.LoadFrom(fDll);
                            ret.Add(GetTypes(typeof(Program), assembly).ToArray());
                        }
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
    }
}

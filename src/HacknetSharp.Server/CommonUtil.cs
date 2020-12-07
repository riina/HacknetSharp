using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using HacknetSharp.Events.Server;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace HacknetSharp.Server
{
    public static class CommonUtil
    {
        static CommonUtil()
        {
            var types = LoadTypesFromFolder(CommonConstants.ExtensionsFolder,
                new[] {typeof(Program), typeof(Service)});
            _customPrograms = new HashSet<Type>(types[1]);
            _customServices = new HashSet<Type>(types[0]);
        }

        private static readonly HashSet<Type> _customPrograms;
        private static readonly HashSet<Type> _customServices;

        public static IEnumerable<Type> CustomPrograms => _customPrograms;
        public static IEnumerable<Type> CustomServices => _customServices;

        public static IEnumerable<Type> GetTypes(Type t, Assembly assembly) =>
            assembly.GetTypes().Where(type => IsSubclass(t, type) && !type.IsAbstract);

        private static readonly HashSet<Type> _defaultModels =
            new HashSet<Type>(GetTypes(typeof(Model<>), typeof(Model<>).Assembly));

        private static readonly HashSet<Type> _defaultPrograms =
            new HashSet<Type>(GetTypes(typeof(Program), typeof(Program).Assembly));

        private static readonly HashSet<Type> _defaultServices =
            new HashSet<Type>(GetTypes(typeof(Service), typeof(Service).Assembly));

        public static IEnumerable<Type> DefaultModels => _defaultModels;
        public static IEnumerable<Type> DefaultPrograms => _defaultPrograms;
        public static IEnumerable<Type> DefaultServices => _defaultServices;

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

        /// <summary>
        /// Generate hash for password
        /// </summary>
        /// <param name="password">Password to hash</param>
        /// <param name="iterations">Number of iterations</param>
        /// <param name="hashLength">Length of hash</param>
        /// <param name="salt">Existing salt (optional)</param>
        /// <param name="saltLength">Salt length (ignored if salt provided)</param>
        /// <returns>Salt and hashed password</returns>
        public static (byte[] hash, byte[] salt) HashPassword(string password, int iterations = 10000,
            int hashLength = 256 / 8, byte[]? salt = null, int saltLength = 128 / 8)
        {
            if (salt == null)
            {
                salt = new byte[saltLength];
                using var r = RandomNumberGenerator.Create();
                r.GetBytes(salt);
            }

            byte[] hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, iterations, hashLength);

            return (hash, salt);
        }

        public static IPAddressRange AsRange(IPAddress address) => new IPAddressRange(address);

        public static OutputEvent CreatePromptEvent(ShellProcess shell) =>
            new OutputEvent {Text = $"{UintToAddress(shell.ProgramContext.System.Address)}:{shell.WorkingDirectory}> "};

        public static string UintToAddress(uint value)
        {
            Span<byte> dst = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(dst, value);
            return $"{dst[0]}.{dst[1]}.{dst[2]}.{dst[3]}";
        }

        public static bool TryParseConString(string conString, ushort defaultPort, [NotNullWhen(true)] out string? name,
            [NotNullWhen(true)] out string? host, out ushort port, [NotNullWhen(false)] out string? error) =>
            Util.TryParseConString(conString, defaultPort, out name!, out host!, out port, out error!);

        public static bool ValidatePassword(string pass, byte[] hash, byte[] salt)
        {
            var (genHash, _) = HashPassword(pass, salt: salt);
            return genHash.AsSpan().SequenceEqual(hash);
        }
    }
}

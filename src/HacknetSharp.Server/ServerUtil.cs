using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Utility class for server-related operations.
    /// </summary>
    public static class ServerUtil
    {
        static ServerUtil()
        {
            var types = LoadTypesFromFolder(ServerConstants.ExtensionsFolder,
                new[] {typeof(Program), typeof(Service)});
            _customPrograms = new HashSet<Type>(types[1]);
            _customServices = new HashSet<Type>(types[0]);
        }

        private static readonly HashSet<Type> _customPrograms;
        private static readonly HashSet<Type> _customServices;

        /// <summary>
        /// Custom programs, as detected by default search.
        /// </summary>
        public static IEnumerable<Type> CustomPrograms => _customPrograms;

        /// <summary>
        /// Custom services, as detected by default search.
        /// </summary>
        public static IEnumerable<Type> CustomServices => _customServices;

        /// <summary>
        /// Searches for concrete subclasses of a given type.
        /// </summary>
        /// <param name="t">Type to find subclasses of.</param>
        /// <param name="assembly">Assembly to search.</param>
        /// <returns>Enumeration over subclasses.</returns>
        public static IEnumerable<Type> GetTypes(Type t, Assembly assembly) =>
            assembly.GetTypes().Where(type => IsSubclass(t, type) && !type.IsAbstract);

        private static readonly HashSet<Type> _defaultModels =
            new(GetTypes(typeof(Model<>), typeof(Model<>).Assembly));

        private static readonly HashSet<Type> _defaultPrograms =
            new(GetTypes(typeof(Program), typeof(Program).Assembly));

        private static readonly HashSet<Type> _defaultServices =
            new(GetTypes(typeof(Service), typeof(Service).Assembly));

        /// <summary>
        /// Standard model types.
        /// </summary>
        public static IEnumerable<Type> DefaultModels => _defaultModels;

        /// <summary>
        /// Standard programs.
        /// </summary>
        public static IEnumerable<Type> DefaultPrograms => _defaultPrograms;

        /// <summary>
        /// Standard services.
        /// </summary>
        public static IEnumerable<Type> DefaultServices => _defaultServices;

        /// <summary>
        /// Load types from a folder
        /// </summary>
        /// <param name="folder">Search folder</param>
        /// <param name="types">Types to search</param>
        /// <returns>List of lists of types</returns>
        public static List<List<Type>> LoadTypesFromFolder(string folder, IReadOnlyList<Type> types)
        {
            List<List<Type>> ret = new();
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

        /// <summary>
        /// Checks if one class is a subclass of another.
        /// </summary>
        /// <param name="base">Base type to test against.</param>
        /// <param name="toCheck">Potential derived type to verify.</param>
        /// <returns>True if <paramref name="toCheck"/> is a subclass of <paramref name="base"/>.</returns>
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

        /// <summary>
        /// Creates a prompt <see cref="OutputEvent"/> for a shell.
        /// </summary>
        /// <param name="shell">Shell to use.</param>
        /// <returns>Output event with formatted address and CWD.</returns>
        public static ShellPromptEvent CreatePromptEvent(ShellProcess shell) =>
            new() {Address = shell.ProgramContext.System.Address, WorkingDirectory = shell.WorkingDirectory};

        /// <summary>
        /// Attempts to parse a connection string (e.g. user@host[:port]).
        /// </summary>
        /// <param name="conString">Connection string to parse.</param>
        /// <param name="defaultPort">Default port to use in absence of port in connection string.</param>
        /// <param name="user">Parsed user.</param>
        /// <param name="host">Parsed host.</param>
        /// <param name="port">Parsed port.</param>
        /// <param name="error">Error message.</param>
        /// <param name="impliedUser">User to use in absence of user in connection string.</param>
        /// <param name="impliedHost">Host to use in absence of host in connection string.</param>
        /// <returns>True if successfully parsed.</returns>
        public static bool TryParseConString(string conString, ushort defaultPort, [NotNullWhen(true)] out string? user,
            [NotNullWhen(true)] out string? host, out ushort port, [NotNullWhen(false)] out string? error,
            string? impliedUser = null, string? impliedHost = null) =>
            Util.TryParseConString(conString, defaultPort, out user!, out host!, out port, out error!, impliedUser,
                impliedHost);

        /// <summary>
        /// Attempts to parse SCP variant of a connection string (e.g. user@host[:path]).
        /// </summary>
        /// <param name="conString">Connection string to parse.</param>
        /// <param name="user">Parsed user.</param>
        /// <param name="host">Parsed host.</param>
        /// <param name="path">Parsed path.</param>
        /// <param name="error">Error message.</param>
        /// <param name="impliedUser">User to use in absence of user in connection string.</param>
        /// <param name="impliedHost">Host to use in absence of host in connection string.</param>
        /// <returns>True if successfully parsed.</returns>
        public static bool TryParseScpConString(string conString, [NotNullWhen(true)] out string? user,
            [NotNullWhen(true)] out string? host, [NotNullWhen(true)] out string? path,
            [NotNullWhen(false)] out string? error, string? impliedUser = null, string? impliedHost = null) =>
            Util.TryParseScpConString(conString, out user!, out host!, out path!, out error!, impliedUser,
                impliedHost);

        /// <summary>
        /// Validates a password against a known hash and salt.
        /// </summary>
        /// <param name="pass">Password to validate.</param>
        /// <param name="hash">Existing hash.</param>
        /// <param name="salt">Existing salt.</param>
        /// <returns>True if password matches hash with specified salt.</returns>
        public static bool ValidatePassword(string pass, byte[] hash, byte[] salt)
        {
            var (genHash, _) = HashPassword(pass, salt: salt);
            return genHash.AsSpan().SequenceEqual(hash);
        }

        [ThreadStatic] private static Random? _random;

        private static Random Random => _random ??= new Random();

        /// <summary>
        /// Randomly selects a value given a mapping of values to weights.
        /// </summary>
        /// <param name="source">Mapping.</param>
        /// <returns>Random value based on weights.</returns>
        public static string SelectWeighted(this Dictionary<string, float> source)
        {
            double s = 0;
            double r = source.Values.Sum() * Random.NextDouble();
            foreach (var kvp in source)
                if (r < (s += kvp.Value))
                    return kvp.Key;
            return source.First().Key;
        }

        private static readonly char[] _userChars =
            Enumerable.Range('0', '9' - '0' + 1)
                .Concat(Enumerable.Range('a', 'z' - 'a' + 1))
                .Select(i => (char)i).ToArray();

        /// <summary>
        /// Generates a random username.
        /// </summary>
        /// <param name="preferredLength">Preferred username length.</param>
        /// <returns>Generated username.</returns>
        public static string GenerateUser(int preferredLength = 16)
        {
            int top = _userChars.Length;
            preferredLength = Math.Min(32, preferredLength);
            Span<char> chars = stackalloc char[preferredLength];
            for (int i = 0; i < preferredLength; i++)
                chars[i] = _userChars[Random.Next(0, top)];
            return new string(chars);
        }

        private static readonly char[] _passChars =
            new int[] {'!', '#', '%', '&', '*'}
                .Concat(new int[] {'!', '#', '%', '&', '*'})
                .Concat(new int[] {'!', '#', '%', '&', '*'})
                .Concat(new int[] {'!', '#', '%', '&', '*'})
                .Concat(new int[] {'!', '#', '%', '&', '*'})
                .Concat(new int[] {'!', '#', '%', '&', '*'})
                .Concat(new int[] {'!', '#', '%', '&', '*'})
                .Concat(new int[] {'!', '#', '%', '&', '*'})
                .Concat(new int[] {'!', '#', '%', '&', '*'})
                .Concat(Enumerable.Range('0', '9' - '0' + 1))
                .Concat(Enumerable.Range('0', '9' - '0' + 1))
                .Concat(Enumerable.Range('0', '9' - '0' + 1))
                .Concat(Enumerable.Range('0', '9' - '0' + 1))
                .Concat(Enumerable.Range('0', '9' - '0' + 1))
                .Concat(Enumerable.Range('0', '9' - '0' + 1))
                .Concat(Enumerable.Range('a', 'z' - 'a' + 1))
                .Concat(Enumerable.Range('A', 'Z' - 'A' + 1))
                .Select(i => (char)i).ToArray();

        /// <summary>
        /// Generates a random password.
        /// </summary>
        /// <param name="preferredLength">Preferred password length.</param>
        /// <returns>Generated password.</returns>
        public static string GeneratePassword(int preferredLength = 16)
        {
            int top = _passChars.Length;
            preferredLength = Math.Min(32, preferredLength);
            Span<char> chars = stackalloc char[preferredLength];
            for (int i = 0; i < preferredLength; i++)
                chars[i] = _passChars[Random.Next(0, top)];
            return new string(chars);
        }

        /// <summary>
        /// Initializes a program context.
        /// </summary>
        /// <param name="world">Target world.</param>
        /// <param name="operationId">Operation ID, if applicable.</param>
        /// <param name="user">Target user.</param>
        /// <param name="person">Target person.</param>
        /// <param name="login">Target login.</param>
        /// <param name="line">Split command line (argv).</param>
        /// <param name="invocationType">Invocation type for this program.</param>
        /// <param name="conWidth">Console width.</param>
        /// <returns>Generated program context.</returns>
        public static ProgramContext InitProgramContext(IWorld world, Guid operationId, IPersonContext user,
            PersonModel person, LoginModel login, string[] line,
            ProgramContext.InvocationType invocationType = ProgramContext.InvocationType.Standard, int conWidth = -1)
        {
            return new()
            {
                World = world,
                Person = person,
                User = user,
                OperationId = operationId,
                Argv = line,
                Type = invocationType,
                ConWidth = conWidth,
                System = login.System,
                Login = login
            };
        }

        /// <summary>
        /// Create a tentative program context for later use in a world in <see cref="IWorld.ExecuteCommand"/>.
        /// </summary>
        /// <param name="world">Target world.</param>
        /// <param name="operationId">Operation ID, if applicable.</param>
        /// <param name="user">Target user.</param>
        /// <param name="person">Target person.</param>
        /// <param name="line">Split command line (argv).</param>
        /// <param name="invocationType">Invocation type for this program.</param>
        /// <param name="conWidth">Console width.</param>
        /// <returns>Generated program context.</returns>
        public static ProgramContext InitTentativeProgramContext(IWorld world, Guid operationId, IPersonContext user,
            PersonModel person, string[] line,
            ProgramContext.InvocationType invocationType = ProgramContext.InvocationType.Standard, int conWidth = -1)
        {
            return new()
            {
                World = world,
                Person = person,
                User = user,
                OperationId = operationId,
                Argv = line,
                Type = invocationType,
                ConWidth = conWidth
            };
        }

        /// <summary>
        /// Splits a command line into its components.
        /// </summary>
        /// <param name="line">String to split.</param>
        /// <returns>Command line with separated components.</returns>
        public static string[] SplitCommandLine(string line)
        {
            int count = line.Length;
            var state = State.Void;
            int entryCount = 0;
            for (int i = 0; i < count; i++)
            {
                char c = line[i];
                switch (state)
                {
                    case State.Void:
                        if (c == '"') state = State.Quoted;
                        else if (c == '\\') state = State.EscapeNonQuoted;
                        else if (!char.IsWhiteSpace(c)) state = State.NonQuoted;

                        break;
                    case State.NonQuoted:
                        if (c == '\\') state = State.EscapeNonQuoted;
                        else if (char.IsWhiteSpace(c))
                        {
                            state = State.Void;
                            entryCount++;
                        }

                        break;
                    case State.Quoted:
                        if (c == '\\')
                            state = State.EscapeQuoted;
                        else if (c == '"')
                        {
                            state = State.Void;
                            entryCount++;
                        }

                        break;
                    case State.EscapeNonQuoted:
                        state = State.NonQuoted;
                        break;
                    case State.EscapeQuoted:
                        state = State.Quoted;
                        break;
                }
            }

            if (state != State.Void)
                entryCount++;

            var res = new string[entryCount];
            var sb = new StringBuilder();
            state = State.Void;
            entryCount = 0;
            for (int i = 0; i < count; i++)
            {
                char c = line[i];
                switch (state)
                {
                    case State.Void:
                        if (c == '"')
                            state = State.Quoted;
                        else if (c == '\\')
                            state = State.EscapeNonQuoted;
                        else if (!char.IsWhiteSpace(c))
                        {
                            state = State.NonQuoted;
                            sb.Append(c);
                        }

                        break;
                    case State.NonQuoted:
                        if (c == '\\')
                            state = State.EscapeNonQuoted;
                        else if (!char.IsWhiteSpace(c))
                            sb.Append(c);
                        else
                        {
                            state = State.Void;
                            res[entryCount] = sb.ToString();
                            entryCount++;
                            sb.Clear();
                        }

                        break;
                    case State.Quoted:
                        if (c == '\\')
                            state = State.EscapeQuoted;
                        else if (c != '"')
                            sb.Append(c);
                        else
                        {
                            state = State.Void;
                            res[entryCount] = sb.ToString();
                            entryCount++;
                            sb.Clear();
                        }

                        break;
                    case State.EscapeNonQuoted:
                        if (c != '"')
                            sb.Append('\\');
                        sb.Append(c);
                        state = State.NonQuoted;
                        break;
                    case State.EscapeQuoted:
                        if (c != '"')
                            sb.Append('\\');
                        sb.Append(c);
                        state = State.Quoted;
                        break;
                }
            }

            if (state != State.Void)
                res[entryCount] = sb.ToString();
            return res;
        }

        private enum State : byte
        {
            Void,
            NonQuoted,
            Quoted,
            EscapeNonQuoted,
            EscapeQuoted
        }
    }
}

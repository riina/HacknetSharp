using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
                new[] { typeof(Program), typeof(Service) });
            s_customPrograms = new HashSet<Type>(types[1]);
            s_customServices = new HashSet<Type>(types[0]);
        }

        private static readonly HashSet<Type> s_customPrograms;
        private static readonly HashSet<Type> s_customServices;

        /// <summary>
        /// Custom programs, as detected by default search.
        /// </summary>
        public static IEnumerable<Type> CustomPrograms => s_customPrograms;

        /// <summary>
        /// Custom services, as detected by default search.
        /// </summary>
        public static IEnumerable<Type> CustomServices => s_customServices;

        /// <summary>
        /// Searches for concrete subclasses of a given type.
        /// </summary>
        /// <param name="t">Type to find subclasses of.</param>
        /// <param name="assembly">Assembly to search.</param>
        /// <returns>Enumeration over subclasses.</returns>
        public static IEnumerable<Type> GetTypes(Type t, Assembly assembly) =>
            assembly.GetTypes().Where(type => IsSubclass(t, type) && !type.IsAbstract);

        private static readonly HashSet<Type> s_defaultPrograms = new(GetTypes(typeof(Program), typeof(Program).Assembly));

        private static readonly HashSet<Type> s_defaultServices = new(GetTypes(typeof(Service), typeof(Service).Assembly));

        private static readonly HashSet<Type> s_defaultPlugins = new(GetTypes(typeof(IWorldPlugin), typeof(IWorldPlugin).Assembly));

        /// <summary>
        /// Standard programs.
        /// </summary>
        public static IEnumerable<Type> DefaultPrograms => s_defaultPrograms;

        /// <summary>
        /// Standard services.
        /// </summary>
        public static IEnumerable<Type> DefaultServices => s_defaultServices;

        /// <summary>
        /// Standard plugins.
        /// </summary>
        public static IEnumerable<Type> DefaultPlugins => s_defaultPlugins;

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
            var opts = new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive };
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
                : toCheck != null && @base.IsAssignableFrom(toCheck));

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
            new() { Address = shell.ProgramContext.System.Address, TargetConnected = shell.Target != null, TargetAddress = shell.Target?.Address ?? 0, WorkingDirectory = shell.WorkingDirectory };

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
        public static bool TryParseConString(string? conString, ushort defaultPort,
            [NotNullWhen(true)] out string? user,
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

        [ThreadStatic] private static Random? s_random;

        private static Random Random => s_random ??= new Random();

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

        private static readonly char[] s_userChars =
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
            int top = s_userChars.Length;
            preferredLength = Math.Min(32, preferredLength);
            Span<char> chars = stackalloc char[preferredLength];
            for (int i = 0; i < preferredLength; i++)
                chars[i] = s_userChars[Random.Next(0, top)];
            return new string(chars);
        }

        private static readonly char[] s_passChars =
            new int[] { '!', '#', '%', '&', '*' }
                .Concat(new int[] { '!', '#', '%', '&', '*' })
                .Concat(new int[] { '!', '#', '%', '&', '*' })
                .Concat(new int[] { '!', '#', '%', '&', '*' })
                .Concat(new int[] { '!', '#', '%', '&', '*' })
                .Concat(new int[] { '!', '#', '%', '&', '*' })
                .Concat(new int[] { '!', '#', '%', '&', '*' })
                .Concat(new int[] { '!', '#', '%', '&', '*' })
                .Concat(new int[] { '!', '#', '%', '&', '*' })
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
            int top = s_passChars.Length;
            preferredLength = Math.Min(32, preferredLength);
            Span<char> chars = stackalloc char[preferredLength];
            for (int i = 0; i < preferredLength; i++)
                chars[i] = s_passChars[Random.Next(0, top)];
            return new string(chars);
        }

        private static readonly char[] s_firewallChars =
            Enumerable.Range('1', '9' - '1' + 1)
                .Concat(Enumerable.Range('1', '9' - '1' + 1))
                .Concat(Enumerable.Range('1', '9' - '1' + 1))
                .Concat(Enumerable.Range('a', 'z' - 'a' + 1))
                .Select(i => (char)i).ToArray();

        /// <summary>
        /// Generates a firewall solution string.
        /// </summary>
        /// <param name="length">Firewall solution length.</param>
        /// <returns>Generated solution.</returns>
        public static string GenerateFirewallSolution(int length)
        {
            int top = s_firewallChars.Length;
            length = Math.Min(32, length);
            Span<char> chars = stackalloc char[length];
            for (int i = 0; i < length; i++)
                chars[i] = s_firewallChars[Random.Next(0, top)];
            return new string(chars);
        }


        /// <summary>
        /// Generates firewall analysis strings.
        /// </summary>
        /// <param name="solution">Firewall solution.</param>
        /// <param name="iterations">Current firewall iterations.</param>
        /// <param name="length">Firewall analyzer length.</param>
        /// <returns>Generated analysis strings.</returns>
        public static string[] GenerateFirewallAnalysis(string solution, int iterations, int length)
        {
            int top = s_firewallChars.Length;
            int count = solution.Length;
            iterations = Math.Clamp(iterations, 0, count);
            string[] res = new string[solution.Length];
            length = Math.Max(Math.Min(32, length), count + 1);
            Span<char> chars = stackalloc char[length];
            int obfuscatedLength = length * (count - iterations) / count;
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < obfuscatedLength; j++)
                    chars[j] = s_firewallChars[Random.Next(0, top)];
                chars.Slice(obfuscatedLength).Fill('0');
                if (count - iterations <= i)
                    chars[Random.Next(0, length)] = solution[i];
                res[i] = new string(chars);
            }

            return res;
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
                Args = line.UnsplitCommandLine().GetArgs(),
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
        /// <param name="command">Command line (argv).</param>
        /// <param name="invocationType">Invocation type for this program.</param>
        /// <param name="conWidth">Console width.</param>
        /// <returns>Generated program context.</returns>
        public static ProgramContext InitTentativeProgramContext(IWorld world, Guid operationId, IPersonContext user,
            PersonModel person, string command,
            ProgramContext.InvocationType invocationType = ProgramContext.InvocationType.Standard, int conWidth = -1)
        {
            return new()
            {
                World = world,
                Person = person,
                User = user,
                OperationId = operationId,
                Args = command.GetArgs(),
                Argv = command.SplitCommandLine(),
                Type = invocationType,
                ConWidth = conWidth
            };
        }

        /// <summary>
        /// Gets millisecond timestamp from a floating-point second value.
        /// </summary>
        /// <param name="time">Value in seconds.</param>
        /// <returns>Millisecond-floor timestamp.</returns>
        public static long GetTimestamp(double time) => (long)Math.Floor(time * 1000.0);

        /// <summary>
        /// Gets hexadecimal millisecond timestamp from a floating-point second value.
        /// </summary>
        /// <param name="time">Value in seconds.</param>
        /// <returns>Hexadecimal millisecond-floor timestamp.</returns>
        public static string GetHexTimestamp(double time) => $"{(long)Math.Floor(time * 1000.0):X16}";

        /// <summary>
        /// Isolate flags from argument list.
        /// </summary>
        /// <param name="arguments">Argument lines to sort.</param>
        /// <param name="optKeys">Option keys.</param>
        /// <returns>Flags, options, and arguments.</returns>
        public static (HashSet<string> flags, Dictionary<string, string> opts, List<string> args) IsolateFlags(
            IReadOnlyList<string> arguments, IReadOnlyCollection<string>? optKeys = null)
        {
            optKeys ??= Array.Empty<string>();
            HashSet<string> flags = new();
            Dictionary<string, string> opts = new();
            List<string> args = new();
            bool argTime = false;
            for (int i = 0; i < arguments.Count; i++)
            {
                string str = arguments[i];
                if (argTime)
                {
                    flags.Add(str);
                    continue;
                }

                if (str.Length < 2 || str[0] != '-')
                {
                    args.Add(str);
                    continue;
                }

                if (str[1] == '-')
                {
                    if (str.Length == 2)
                    {
                        argTime = true;
                        continue;
                    }

                    string id = str[2..];
                    if (optKeys.Contains(id))
                    {
                        if (TryGetArg(arguments, i, out string? res))
                            opts[id] = res;
                        i++;
                    }
                    else
                        flags.Add(id);
                }
                else
                {
                    string firstId = str[1].ToString();
                    if (str.Length == 2 && optKeys.Contains(firstId))
                    {
                        if (TryGetArg(arguments, i, out string? res))
                            opts[firstId] = res;
                        i++;
                    }
                    else
                        flags.UnionWith(str.Skip(1).Select(c => c.ToString()));
                }
            }

            return (flags, opts, args);
        }

        private static bool TryGetArg(IReadOnlyList<string> list, int i, [NotNullWhen(true)] out string? arg)
        {
            if (i + 1 < list.Count)
            {
                arg = list[i + 1];
                return true;
            }

            arg = null;
            return false;
        }

        /// <summary>
        /// Escapes a command line element.
        /// </summary>
        /// <param name="str">Element to escape.</param>
        /// <returns>Escaped element.</returns>
        public static string EscapeCommandLineElement(this string str)
        {
            if (str.Contains(' ') || str.Contains('\t') || str.Contains('\n'))
                return $"\"{str.Replace("\"", "\\\"")}\"";
            else
                return str.Replace("\"", "\\\"");
        }

        /// <summary>
        /// Un-splits a command line.
        /// </summary>
        /// <param name="line">Line to unsplit.</param>
        /// <returns>Unsplit line.</returns>
        public static string UnsplitCommandLine(this string[] line)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < line.Length; i++)
            {
                string str = line[i];
                if (str.Contains(' ') || str.Contains('\t') || str.Contains('\n'))
                    sb.Append('"').Append(str.Replace("\"", "\\\"")).Append('"');
                else
                    sb.Append(str.Replace("\"", "\\\""));
                if (i + 1 != line.Length)
                    sb.Append(' ');
            }

            return sb.ToString();
        }

        private static string GetArgs(this string command)
        {
            int count = command.CountCommandLineElements();
            string args;
            if (count < 2)
                args = "";
            else
            {
                (Range range, bool quoted)[] splits = new (Range range, bool quoted)[2];
                command.DivideCommandLineElements(splits);
                (Range range, bool quoted) = splits[1];
                var start = range.Start;
                if (quoted)
                    start = new Index(start.Value - 1, start.IsFromEnd);
                args = command[start..];
            }

            return args;
        }

        /// <summary>
        /// Gets the ranges created when splitting a command line into its components.
        /// </summary>
        /// <param name="line">String to split.</param>
        /// <param name="target">Pre-allocated array (parsing ends when array is filled).</param>
        /// <returns>Input array.</returns>
        public static (Range range, bool quoted)[] DivideCommandLineElements(this string line,
            (Range range, bool quoted)[]? target = null)
        {
            target ??= new (Range range, bool quoted)[line.CountCommandLineElements()];
            if (target.Length == 0) return target;
            int count = line.Length;
            var state = State.Void;
            int baseIdx = 0;
            int arrayIdx = 0;
            for (int i = 0; i < count; i++)
            {
                char c = line[i];
                switch (state)
                {
                    case State.Void:
                        baseIdx = i;
                        if (c == '"')
                        {
                            baseIdx++;
                            state = State.Quoted;
                        }
                        else if (c == '\\')
                            state = State.EscapeNonQuoted;
                        else if (!char.IsWhiteSpace(c))
                            state = State.NonQuoted;

                        break;
                    case State.NonQuoted:
                        if (c == '\\')
                            state = State.EscapeNonQuoted;
                        else if (char.IsWhiteSpace(c))
                        {
                            state = State.Void;
                            target[arrayIdx] = (new Range(baseIdx, i), false);
                            arrayIdx++;
                            if (target.Length <= arrayIdx) return target;
                        }

                        break;
                    case State.Quoted:
                        if (c == '\\')
                            state = State.EscapeQuoted;
                        else if (c == '"')
                        {
                            state = State.Void;
                            target[arrayIdx] = (new Range(baseIdx, i), true);
                            arrayIdx++;
                            if (target.Length <= arrayIdx) return target;
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

            if (target.Length > arrayIdx && state != State.Void)
                target[arrayIdx] = (new Range(baseIdx, count), state == State.Quoted || state == State.EscapeQuoted);
            return target;
        }

        /// <summary>
        /// Obtains a string from the specified range.
        /// </summary>
        /// <param name="line">String to operate on.</param>
        /// <param name="range">Range and quote type.</param>
        /// <param name="sb">Preexisting string builder.</param>
        /// <returns>Component string.</returns>
        public static string SliceCommandLineElement(this string line, (Range range, bool quoted) range,
            StringBuilder? sb = null)
        {
            sb ??= new StringBuilder();
            var segment = line.AsSpan()[range.range];
            int count = segment.Length;
            var state = range.quoted ? State.Quoted : State.NonQuoted;
            for (int i = 0; i < count; i++)
            {
                char c = segment[i];
                switch (state)
                {
                    /*case State.Void:
                        if (c == '"')
                            state = State.Quoted;
                        else if (c == '\\')
                            state = State.EscapeNonQuoted;
                        else if (!char.IsWhiteSpace(c))
                        {
                            state = State.NonQuoted;
                            sb.Append(c);
                        }

                        break;*/
                    case State.NonQuoted:
                        if (c == '\\')
                            state = State.EscapeNonQuoted;
                        else if (!char.IsWhiteSpace(c))
                            sb.Append(c);
                        else
                            return sb.ToString();

                        break;
                    case State.Quoted:
                        if (c == '\\')
                            state = State.EscapeQuoted;
                        else if (c != '"')
                            sb.Append(c);
                        else
                            return sb.ToString();

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

            if (state == State.EscapeQuoted || state == State.EscapeNonQuoted)
                sb.Append('\\');
            return sb.ToString();
        }

        /// <summary>
        /// Counts the number of command line elements in a string.
        /// </summary>
        /// <param name="line">String to split.</param>
        /// <returns>Number of command line elements.</returns>
        public static int CountCommandLineElements(this string line)
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
            return entryCount;
        }

        /// <summary>
        /// Splits a command line into its components.
        /// </summary>
        /// <param name="line">String to split.</param>
        /// <returns>Command line with separated components.</returns>
        public static string[] SplitCommandLine(this string line)
        {
            int count = line.Length;

            int entryCount = line.CountCommandLineElements();
            string[] res = new string[entryCount];
            var sb = new StringBuilder();
            var state = State.Void;
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

            if (state == State.EscapeQuoted || state == State.EscapeNonQuoted)
                sb.Append('\\');
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

        /// <summary>
        /// Reads a base64-encoded object via a stream transform.
        /// </summary>
        /// <param name="content">Encoded object.</param>
        /// <param name="func">Transform.</param>
        /// <typeparam name="T">Type.</typeparam>
        /// <returns>Object or null.</returns>
        public static T ReadBase64Azura<T>(string content, Func<Stream, T> func)
        {
            return func(new MemoryStream(Convert.FromBase64String(content)));
        }

        /// <summary>
        /// Writes a base64-encoded object via a stream transform.
        /// </summary>
        /// <param name="action">Transform.</param>
        /// <returns>Base64.</returns>
        public static string WriteBase64Azura(Action<Stream> action)
        {
            MemoryStream memoryStream = new();
            action(memoryStream);
            return Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
        }
    }
}

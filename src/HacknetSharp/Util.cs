using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HacknetSharp
{
    /// <summary>
    /// Contains utility methods and extension methods.
    /// </summary>
    public static partial class Util
    {
        /// <summary>
        /// Shorthand for ConfigureAwait(false).
        /// </summary>
        /// <param name="task">Task to wrap.</param>
        /// <returns>Wrapped task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConfiguredTaskAwaitable Caf(this Task task) => task.ConfigureAwait(false);

        /// <summary>
        /// Shorthand for ConfigureAwait(false).
        /// </summary>
        /// <param name="task">Task to wrap.</param>
        /// <returns>Wrapped task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConfiguredTaskAwaitable<T> Caf<T>(this Task<T> task) => task.ConfigureAwait(false);


        /// <summary>
        /// Writes a command code to this stream.
        /// </summary>
        /// <param name="stream">Stream to operate on.</param>
        /// <param name="command">Command to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteCommand(this Stream stream, Command command) =>
            uintSerialization.Serialize((uint)command, stream);

        /// <summary>
        /// Reads a command code from this stream.
        /// </summary>
        /// <param name="stream">Stream to operate on.</param>
        /// <returns>Read command.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Command ReadCommand(this Stream stream) =>
            (Command)uintSerialization.Deserialize(stream);

        /// <summary>
        /// Asynchronously reads a command code from this stream.
        /// </summary>
        /// <param name="stream">Stream to operate on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task that represents pending read command.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Command> ReadCommandAsync(this Stream stream, CancellationToken cancellationToken)
        {
            await Task.Yield();
            return (Command)uintSerialization.Deserialize(stream);
        }

        /// <summary>
        /// Triggers a transition to another lifecycle state.
        /// </summary>
        /// <param name="resetEvent">Concurrency event to use.</param>
        /// <param name="min">Minimum input state.</param>
        /// <param name="max">Maximum input state.</param>
        /// <param name="target">State to set target reference to.</param>
        /// <param name="toChange">Target reference to modify.</param>
        /// <returns>Input state.</returns>
        /// <exception cref="InvalidOperationException">Invalid input state based on bounds.</exception>
        public static LifecycleState TriggerState(AutoResetEvent resetEvent, LifecycleState min, LifecycleState max,
            LifecycleState target, ref LifecycleState toChange)
        {
            resetEvent.WaitOne();
            try
            {
                var res = toChange;
                RequireState(toChange, min, max);
                toChange = target;
                return res;
            }
            finally
            {
                resetEvent.Set();
            }
        }

        /// <summary>
        /// Triggers a transition to another lifecycle state based on a mapping.
        /// </summary>
        /// <param name="resetEvent">Concurrency event to use.</param>
        /// <param name="map">Transition mapping.</param>
        /// <param name="toChange">Target reference to modify.</param>
        /// <returns>Input state.</returns>
        /// <exception cref="KeyNotFoundException">Input state was not found in transition dictionary.</exception>
        public static LifecycleState TriggerState(AutoResetEvent resetEvent,
            Dictionary<LifecycleState, LifecycleState> map, ref LifecycleState toChange)
        {
            resetEvent.WaitOne();
            try
            {
                var res = toChange;
                if (!map.TryGetValue(res, out var target))
                    throw new KeyNotFoundException($"No mapping to state {res}");
                toChange = target;
                return res;
            }
            finally
            {
                resetEvent.Set();
            }
        }

        /// <summary>
        /// Evaluates active state and throws <see cref="InvalidOperationException"/> if requirement is breached.
        /// </summary>
        /// <param name="state">Active state of object.</param>
        /// <param name="min">Minimum state.</param>
        /// <param name="max">Maximum state.</param>
        /// <exception cref="InvalidOperationException">Invalid input state based on bounds.</exception>
        public static void RequireState(LifecycleState state, LifecycleState min, LifecycleState max)
        {
            if ((int)state < (int)min)
                throw new InvalidOperationException(
                    $"Cannot perform this action that requires state {min} when object is in state {state}");
            if ((int)state > (int)max)
                throw new InvalidOperationException(
                    $"Cannot perform this action that requires state {max} when object is in state {state}");
        }

        /// <summary>
        /// Attempts to increment a countdown based on an expectation of the current lifecycle state.
        /// </summary>
        /// <param name="resetEvent">Concurrency event to use.</param>
        /// <param name="countdownEvent">Countdown to modify.</param>
        /// <param name="state">Active state of object.</param>
        /// <param name="min">Minimum state.</param>
        /// <param name="max">Maximum state.</param>
        /// <returns>True if state limits were met and countdown was incremented.</returns>
        public static bool TryIncrementCountdown(AutoResetEvent resetEvent, CountdownEvent countdownEvent,
            LifecycleState state, LifecycleState min, LifecycleState max)
        {
            resetEvent.WaitOne();
            bool keepGoing = !((int)state < (int)min || (int)state > (int)max);
            if (keepGoing)
                countdownEvent.AddCount();
            resetEvent.Set();
            return keepGoing;
        }

        /// <summary>
        /// Decrements a countdown, synchronized using a concurrency event.
        /// </summary>
        /// <param name="resetEvent">Concurrency event to use.</param>
        /// <param name="countdownEvent">Countdown to modify.</param>
        public static void DecrementCountdown(AutoResetEvent resetEvent, CountdownEvent countdownEvent)
        {
            resetEvent.WaitOne();
            countdownEvent.Signal();
            resetEvent.Set();
        }

        static Util()
        {
            _commandT2C = new Dictionary<Type, Command>();
            _commandC2T = new Dictionary<Command, Event>();
            RegisterCommands();
        }

        static partial void RegisterCommands();

        private static void RegisterCommand<TEvent>(Command key) where TEvent : Event, new()
        {
            var type = typeof(TEvent);
            _commandT2C[type] = key;
            _commandC2T[key] = new TEvent();
        }

        private static readonly Dictionary<Type, Command> _commandT2C;
        private static readonly Dictionary<Command, Event> _commandC2T;

        /// <summary>
        /// Reads an event from this stream.
        /// </summary>
        /// <param name="stream">Stream to operate on.</param>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <returns>Read event or null if stream closed.</returns>
        /// <exception cref="ProtocolException">Thrown when an invalid command code was received or deserialization failed.</exception>
        public static TEvent? ReadEvent<TEvent>(this Stream stream) where TEvent : Event
        {
            Command command;
            try
            {
                command = stream.ReadCommand();
            }
            catch
            {
                return null;
            }

            if (!_commandC2T.TryGetValue(command, out var f))
                throw new ProtocolException($"Unknown command type {(uint)command} received");
            Event evt;
            try
            {
                evt = f.Deserialize(stream);
            }
            catch (Exception e)
            {
                throw new ProtocolException(e.Message);
            }

            return evt as TEvent ??
                   throw new Exception(
                       $"Failed to cast event {evt.GetType().FullName} as {typeof(TEvent).FullName}");
        }

        /// <summary>
        /// Asynchronously reads an event from this stream.
        /// </summary>
        /// <param name="stream">Stream to operate on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <typeparam name="TEvent">Event type.</typeparam>
        /// <returns>Task that returns read event or null if stream closed.</returns>
        /// <exception cref="ProtocolException"></exception>
        public static async Task<TEvent?> ReadEventAsync<TEvent>(this Stream stream,
            CancellationToken cancellationToken) where TEvent : Event
        {
            Command command;
            try
            {
                command = await stream.ReadCommandAsync(cancellationToken).Caf();
            }
            catch (EndOfStreamException)
            {
                return null;
            }

            if (!_commandC2T.TryGetValue(command, out var f))
                throw new ProtocolException($"Unknown command type {(uint)command} received");

            var evt = f.Deserialize(stream);
            return evt as TEvent ??
                   throw new Exception(
                       $"Failed to cast event {evt.GetType().FullName} as {typeof(TEvent).FullName}");
        }

        /// <summary>
        /// Writes an event to this stream.
        /// </summary>
        /// <param name="stream">Stream to operate on.</param>
        /// <param name="evt">Event to send.</param>
        /// <exception cref="ArgumentException">Unregistered command type. The protocol depends on specific registered types.</exception>
        public static void WriteEvent(this Stream stream, Event evt)
        {
            if (!_commandT2C.TryGetValue(evt.GetType(), out var command))
                throw new ArgumentException($"Couldn't find registered command type for type {evt.GetType().FullName}");
            stream.WriteCommand(command);
            evt.Serialize(stream);
        }

        /// <summary>
        /// Reads a password from <see cref="Console"/>.
        /// </summary>
        /// <returns></returns>
        public static string? ReadPassword()
        {
            var ss = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.C && (key.Modifiers & ConsoleModifiers.Control) == ConsoleModifiers.Control)
                {
                    return null;
                }

                if (key.Key == ConsoleKey.Backspace)
                {
                    if (ss.Length != 0)
                        ss.Remove(ss.Length - 1, 1);
                    continue;
                }

                ss.Append(key.KeyChar);
            }

            Console.WriteLine();
            return ss.ToString();
        }

        /// <summary>
        /// Reads a SecureString password
        /// </summary>
        /// <returns>Password or null if terminated</returns>
        public static SecureString? ReadSecureString()
        {
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

        /// <summary>
        /// Answers perceived as an affirmative statement for <see cref="Confirm"/>.
        /// </summary>
        public static readonly IReadOnlyCollection<string> YesAnswers = new HashSet<string>(new[] { "yes", "y", "sure", "absolutely", "sin duda", "do it", "yes please", "yes, please", "bingo", "come on", "such" });

        /// <summary>
        /// Asks for confirmation on an action and validates response.
        /// </summary>
        /// <param name="mes">Prompt to print.</param>
        /// <returns>True if user input was contained in <see cref="YesAnswers"/>.</returns>
        public static bool Confirm(string mes)
        {
            Console.WriteLine(mes);
            return YesAnswers.Contains((Console.ReadLine() ?? "").ToLowerInvariant());
        }

        private static readonly Regex _conStringRegex = new(@"([A-Za-z0-9]+)@([\S]+)");
        private static readonly Regex _serverPortRegex = new(@"([^\s:]+):([\S\s]+)");

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
        public static bool TryParseConString(string? conString, ushort defaultPort, out string? user, out string? host,
            out ushort port, out string? error, string? impliedUser = null, string? impliedHost = null)
        {
            user = null;
            host = null;
            port = defaultPort;
            error = null;
            if (conString == null)
            {
                if (impliedUser == null)
                {
                    error = "Missing username";
                    return false;
                }

                if (impliedHost == null)
                {
                    error = "Missing hostname";
                    return false;
                }

                user = impliedUser;
                host = impliedHost;
                return true;
            }

            var conStringMatch = _conStringRegex.Match(conString);
            if (!conStringMatch.Success)
            {
                if (impliedHost == null)
                {
                    error = "Invalid conString, must be user@host[:port]";
                    return false;
                }

                user = null;
                host = impliedHost;
            }
            else
            {
                user = conStringMatch.Groups[1].Value;
                host = conStringMatch.Groups[2].Value;
                conString = host;
            }

            if (!conString.Contains(":"))
            {
                if (!conStringMatch.Success)
                {
                    if (!string.IsNullOrWhiteSpace(conString)) user = conString;
                    else if (impliedUser != null)
                        user = impliedUser;
                    else
                    {
                        error = "Invalid conString, must be user@host[:port]";
                        return false;
                    }

                    return true;
                }

                if (ushort.TryParse(conString, out ushort tmp))
                    port = tmp;
                return true;
            }

            var serverPortMatch = _serverPortRegex.Match(conString);
            if (!serverPortMatch.Success || !ushort.TryParse(serverPortMatch.Groups[2].Value, out port))
            {
                error = "Invalid host/port, must be user@host[:port]";
                return false;
            }

            if (!conStringMatch.Success)
                user = serverPortMatch.Groups[1].Value;
            else
                host = serverPortMatch.Groups[1].Value;

            return true;
        }

        /// <summary>
        /// Attempts to parse SCP variant of a connection string (e.g. user@host:path).
        /// </summary>
        /// <param name="conString">Connection string to parse.</param>
        /// <param name="user">Parsed user.</param>
        /// <param name="host">Parsed host.</param>
        /// <param name="path">Parsed path.</param>
        /// <param name="error">Error message.</param>
        /// <param name="impliedUser">User to use in absence of user in connection string.</param>
        /// <param name="impliedHost">Host to use in absence of host in connection string.</param>
        /// <returns>True if successfully parsed.</returns>
        public static bool TryParseScpConString(string conString, out string? user, out string? host, out string? path,
            out string? error, string? impliedUser = null, string? impliedHost = null)
        {
            user = null;
            host = null;
            path = null;
            error = null;
            var conStringMatch = _conStringRegex.Match(conString);
            if (!conStringMatch.Success)
            {
                if (impliedHost == null)
                {
                    error = "Invalid conString, must be user@host:path";
                    return false;
                }

                user = null;
                host = impliedHost;
            }
            else
            {
                user = conStringMatch.Groups[1].Value;
                host = conStringMatch.Groups[2].Value;
                conString = host;
            }

            if (!conString.Contains(":"))
            {
                if (!conStringMatch.Success)
                {
                    if (impliedUser == null)
                    {
                        error = "Invalid conString, must be user@host:path";
                        return false;
                    }

                    user = impliedUser;
                }

                path = conString;
                return true;
            }

            var serverPortMatch = _serverPortRegex.Match(conString);
            if (!serverPortMatch.Success)
            {
                error = "Invalid host/path, must be user@server:path";
                return false;
            }

            if (!conStringMatch.Success)
                user = serverPortMatch.Groups[1].Value;
            else
                host = serverPortMatch.Groups[1].Value;
            path = serverPortMatch.Groups[2].Value;

            return true;
        }

        private static readonly Regex _replacementRegex = new(@"((?:^|[^\\])(?:\\\\)*){((?:[^{}\\]|\\.)*)}");
        private static readonly Regex _shellReplacementRegex = new(@"\$([A-Za-z0-9]+)");

        /// <summary>
        /// Apply replacement splitting.
        /// </summary>
        /// <param name="source">Source text.</param>
        /// <returns>List of text segments.</returns>
        public static List<(bool replacement, int start, int count)> SplitReplacements(this string source)
        {
            var result = new List<(bool replacement, int start, int count)>();
            Match match;
            int i = 0;
            while ((match = _replacementRegex.Match(source, i)).Success)
            {
                var preGroup = match.Groups[1];
                var postGroup = match.Groups[2];
                if (match.Index != i || preGroup.Length != 0)
                    result.Add((false, i, preGroup.Index + preGroup.Length - i));
                result.Add((true, postGroup.Index, postGroup.Length));
                i = match.Index + match.Length;
            }

            if (i != source.Length)
                result.Add((false, i, source.Length - i));
            return result;
        }

        /// <summary>
        /// Applies replacements using curly bracket syntax.
        /// </summary>
        /// <param name="str">String to apply replacements to.</param>
        /// <param name="replacements">Replacement dictionary to use.</param>
        /// <returns>String with replacements applied.</returns>
        public static string ApplyReplacements(this string str, IReadOnlyDictionary<string, string> replacements) =>
            _replacementRegex.Replace(str,
                m => replacements.TryGetValue(m.Groups[2].Value, out var rep) ? m.Groups[1] + rep : m.Value);

        /// <summary>
        /// Applies replacements using dollar sign syntax.
        /// </summary>
        /// <param name="str">String to apply replacements to.</param>
        /// <param name="replacements">Replacement dictionary to use.</param>
        /// <returns>String with replacements applied.</returns>
        public static string
            ApplyShellReplacements(this string str, IReadOnlyDictionary<string, string> replacements) =>
            _shellReplacementRegex.Replace(str,
                m => replacements.TryGetValue(m.Groups[1].Value, out var rep) ? rep : m.Value);

        /// <summary>
        /// Format an IPv4 address.
        /// </summary>
        /// <param name="value">32-bit unsigned value with highest order byte representing first octet, etc.</param>
        /// <returns>IPv4 address (a.b.c.d).</returns>
        public static string UintToAddress(uint value) =>
            $"{(byte)(value >> 24)}.{(byte)(value >> 16)}.{(byte)(value >> 8)}.{(byte)value}";


        /// <summary>
        /// Formats an alert box.
        /// </summary>
        /// <param name="kind">Alert kind.</param>
        /// <param name="header">Alert header.</param>
        /// <param name="body">Alert body.</param>
        public static StringBuilder FormatAlert(string kind, string header, string body)
        {
            List<string> lines = new(body.Split('\n'));
            lines.Insert(0, $"{kind} : {header.ToUpperInvariant()} ");
            int longest = Math.Min(lines.Select(l => l.Length).Max(), Math.Max(10, Console.BufferWidth - 6));
            lines[0] = lines[0] + new string('-', Math.Max(longest - lines[0].Length, 0));
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
                sb.AppendLine(i == 0
                    ? string.Format(CultureInfo.InvariantCulture,
                        $"»» {{0,{-longest}}} ««", lines[i])
                    : lines[i]);
            sb.Append("»» ").Append('-', longest).AppendLine(" ««");
            return sb;
        }
    }
}

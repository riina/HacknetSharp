using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;
using Ns;

namespace HacknetSharp
{
    /// <summary>
    /// Contains utility methods and extension methods.
    /// </summary>
    public static class Util
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
        /// Shorthand for ConfigureAwait(false).
        /// </summary>
        /// <param name="task">Task to wrap.</param>
        /// <returns>Wrapped task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConfiguredValueTaskAwaitable Caf(this ValueTask task) => task.ConfigureAwait(false);

        /// <summary>
        /// Shorthand for ConfigureAwait(false).
        /// </summary>
        /// <param name="task">Task to wrap.</param>
        /// <returns>Wrapped task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ConfiguredValueTaskAwaitable<T> Caf<T>(this ValueTask<T> task) => task.ConfigureAwait(false);


        /// <summary>
        /// Writes a command code to this stream.
        /// </summary>
        /// <param name="stream">Stream to operate on.</param>
        /// <param name="command">Command to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteCommand(this Stream stream, Command command) =>
            stream.WriteU32((uint)command);

        /// <summary>
        /// Reads a command code from this stream.
        /// </summary>
        /// <param name="stream">Stream to operate on.</param>
        /// <returns>Read command.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Command ReadCommand(this Stream stream) =>
            (Command)stream.ReadU32();

        /// <summary>
        /// Asynchronously reads a command code from this stream.
        /// </summary>
        /// <param name="stream">Stream to operate on.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task that represents pending read command.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Command> ReadCommandAsync(this Stream stream, CancellationToken cancellationToken) =>
            (Command)await stream.ReadU32Async(cancellationToken).Caf();

        /// <summary>
        /// Writes a nullable UTF-8 string to this stream.
        /// </summary>
        /// <param name="stream">Stream to operate on.</param>
        /// <param name="value">Value to write.</param>
        /// <remarks>The string is formatted by the <see cref="NetSerializerExtensions.WriteUtf8String"/> method this method uses.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUtf8StringNullable(this Stream stream, string? value)
        {
            stream.WriteU8(value != null ? (byte)1 : (byte)0);
            if (value != null) stream.WriteUtf8String(value);
        }

        /// <summary>
        /// Reads a nullable UTF-8 string from this stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        /// <remarks>The string must be formatted to be compatible with the <see cref="NetSerializerExtensions.ReadUtf8String"/> method this method uses.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? ReadUtf8StringNullable(this Stream stream)
        {
            return stream.ReadU8() != 0 ? stream.ReadUtf8String() : null;
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
            _commandC2T = new Dictionary<Command, Func<Event>>();
            RegisterCommand<ClientDisconnectEvent>(Command.CS_Disconnect);
            RegisterCommand<CommandEvent>(Command.CS_Command);
            RegisterCommand<InitialCommandEvent>(Command.CS_InitialCommand);
            RegisterCommand<InputResponseEvent>(Command.CS_InputResponse);
            RegisterCommand<LoginEvent>(Command.CS_Login);
            RegisterCommand<RegistrationTokenForgeRequestEvent>(Command.CS_RegistrationTokenForgeRequest);
            RegisterCommand<EditResponseEvent>(Command.CS_EditResponse);

            RegisterCommand<AccessFailEvent>(Command.SC_AccessFail);
            RegisterCommand<FailBaseServerEvent>(Command.SC_FailBaseServer);
            RegisterCommand<InputRequestEvent>(Command.SC_InputRequest);
            RegisterCommand<LoginFailEvent>(Command.SC_LoginFail);
            RegisterCommand<OperationCompleteEvent>(Command.SC_OperationComplete);
            RegisterCommand<OutputEvent>(Command.SC_Output);
            RegisterCommand<RegistrationTokenForgeResponseEvent>(Command.SC_RegistrationTokenForgeResponse);
            RegisterCommand<ServerDisconnectEvent>(Command.SC_Disconnect);
            RegisterCommand<UserInfoEvent>(Command.SC_UserInfo);
            RegisterCommand<EditRequestEvent>(Command.SC_EditRequest);
        }

        private static void RegisterCommand<TEvent>(Command key) where TEvent : Event, new()
        {
            var type = typeof(TEvent);
            _commandT2C[type] = key;
            _commandC2T[key] = () => new TEvent();
        }

        private static readonly Dictionary<Type, Command> _commandT2C;
        private static readonly Dictionary<Command, Func<Event>> _commandC2T;

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
            var obj = f();
            var evt = obj as TEvent ??
                      throw new Exception(
                          $"Failed to cast event {obj.GetType().FullName} as {typeof(TEvent).FullName}");
            try
            {
                evt.Deserialize(stream);
            }
            catch (Exception e)
            {
                throw new ProtocolException(e.Message);
            }

            return evt;
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

            var obj = f();
            var evt = obj as TEvent ??
                      throw new Exception(
                          $"Failed to cast event {obj.GetType().FullName} as {typeof(TEvent).FullName}");
            evt.Deserialize(stream);
            return evt;
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
        public static readonly IReadOnlyCollection<string> _yes = new HashSet<string>(new[]
        {
            "yes", "y", "sure", "absolutely", "sin duda", "do it", "yes please", "yes, please", "bingo", "come on",
            "such"
        });

        /// <summary>
        /// Asks for confirmation on an action and validates response.
        /// </summary>
        /// <param name="mes">Prompt to print.</param>
        /// <returns>True if user input was contained in <see cref="_yes"/>.</returns>
        public static bool Confirm(string mes)
        {
            Console.WriteLine(mes);
            return _yes.Contains((Console.ReadLine() ?? "").ToLowerInvariant());
        }

        private static readonly Regex _conStringRegex = new Regex(@"([A-Za-z0-9]+)@([\S]+)");
        private static readonly Regex _serverPortRegex = new Regex(@"([^\s:]+):([\S\s]+)");

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
        public static bool TryParseConString(string conString, ushort defaultPort, out string? user, out string? host,
            out ushort port, out string? error, string? impliedUser = null, string? impliedHost = null)
        {
            user = null;
            host = null;
            port = defaultPort;
            error = null;
            var conStringMatch = _conStringRegex.Match(conString);
            if (!conStringMatch.Success)
            {
                if (impliedUser == null)
                {
                    error = "Invalid conString, must be user@host[:port]";
                    return false;
                }

                user = impliedUser;
                host = conString;
            }
            else
            {
                user = conStringMatch.Groups[1].Value;
                host = conStringMatch.Groups[2].Value;
            }

            if (!host.Contains(":"))
            {
                if (ushort.TryParse(host, out ushort soloHost))
                {
                    port = soloHost;
                    if (impliedHost != null) host = impliedHost;
                }
                else if (string.IsNullOrWhiteSpace(host) && impliedHost != null) host = impliedHost;

                return true;
            }

            var serverPortMatch = _serverPortRegex.Match(host);
            if (!serverPortMatch.Success || !ushort.TryParse(serverPortMatch.Groups[2].Value, out port))
            {
                error = "Invalid host/port, must be user@host[:port]";
                return false;
            }

            host = serverPortMatch.Groups[1].Value;

            return true;
        }

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
                if (impliedUser == null)
                {
                    error = "Invalid conString, must be user@host:path";
                    return false;
                }

                user = impliedUser;
                host = conString;
            }
            else
            {
                user = conStringMatch.Groups[1].Value;
                host = conStringMatch.Groups[2].Value;
            }

            if (!host.Contains(":"))
            {
                if (!string.IsNullOrWhiteSpace(host) && impliedHost != null)
                {
                    path = host;
                    host = impliedHost;
                    return true;
                }

                error = "Invalid conString, must be user@server:path";
                return false;
            }

            var serverPortMatch = _serverPortRegex.Match(host);
            if (!serverPortMatch.Success)
            {
                error = "Invalid host/port, must be user@server:path";
                return false;
            }

            host = serverPortMatch.Groups[1].Value;
            path = serverPortMatch.Groups[2].Value;

            return true;
        }

        private static readonly Regex _replacementRegex = new Regex(@"{((?:[^{}\\]|\\.)*)}");
        private static readonly Regex _shellReplacementRegex = new Regex(@"\$([A-Za-z0-9]+)");

        /// <summary>
        /// Applies replacements using curly bracket syntax.
        /// </summary>
        /// <param name="str">String to apply replacements to.</param>
        /// <param name="replacements">Replacement dictionary to use.</param>
        /// <returns>String with replacements applied.</returns>
        public static string ApplyReplacements(this string str, IReadOnlyDictionary<string, string> replacements) =>
            _replacementRegex.Replace(str,
                m => replacements.TryGetValue(m.Groups[1].Value, out var rep) ? rep : m.Value);

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
    }
}

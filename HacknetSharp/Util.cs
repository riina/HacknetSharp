using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ns;

namespace HacknetSharp
{
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteCommand(this Stream stream, Command command) =>
            stream.WriteU32((uint)command);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Command ReadCommand(this Stream stream) =>
            (Command)stream.ReadU32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<Command> ReadCommandAsync(this Stream stream, CancellationToken cancellationToken) =>
            (Command)await stream.ReadU32Async(cancellationToken).Caf();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Expect(this Stream stream, Command command, out Command actual)
        {
            actual = (Command)stream.ReadU32();
            return actual == command;
        }

        public static void TriggerState(AutoResetEvent resetEvent, LifecycleState min, LifecycleState max,
            LifecycleState target, ref LifecycleState toChange)
        {
            resetEvent.WaitOne();
            try
            {
                RequireState(toChange, min, max);
                toChange = target;
            }
            finally
            {
                resetEvent.Set();
            }
        }

        public static void RequireState(LifecycleState state, LifecycleState min, LifecycleState max)
        {
            if ((int)state < (int)min)
                throw new InvalidOperationException(
                    $"Cannot perform this action that requires state {min} when object is in state {state}");
            if ((int)state > (int)max)
                throw new InvalidOperationException(
                    $"Cannot perform this action that requires state {max} when object is in state {state}");
        }

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

        public static void DecrementCountdown(AutoResetEvent resetEvent, CountdownEvent countdownEvent)
        {
            resetEvent.WaitOne();
            countdownEvent.Signal();
            resetEvent.Set();
        }

        static Util()
        {
            _commandT2C = new Dictionary<Type, Command>();
            _commandC2T = new Dictionary<Command, Type>();
            foreach (var type in typeof(Event).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Event))))
            {
                if (!(type.GetCustomAttribute(typeof(EventCommandAttribute)) is EventCommandAttribute attr)) continue;
                _commandT2C[type] = attr.Command;
                _commandC2T[attr.Command] = type;
            }
        }

        private static readonly Dictionary<Type, Command> _commandT2C;
        private static readonly Dictionary<Command, Type> _commandC2T;

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

            if (!_commandC2T.TryGetValue(command, out var type))
                throw new ProtocolException($"Unknown command type {(uint)command} received");
            var obj = Activator.CreateInstance(type);
            var evt = obj as TEvent ??
                      throw new Exception(
                          $"Failed to cast event {obj.GetType().FullName} as {typeof(TEvent).FullName}");
            evt.Deserialize(stream);
            return evt;
        }

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

            if (!_commandC2T.TryGetValue(command, out var type))
                throw new ProtocolException($"Unknown command type {(uint)command} received");

            var obj = Activator.CreateInstance(type);
            var evt = obj as TEvent ??
                      throw new Exception(
                          $"Failed to cast event {obj.GetType().FullName} as {typeof(TEvent).FullName}");
            evt.Deserialize(stream);
            return evt;
        }

        public static void WriteEvent(this Stream stream, Event evt)
        {
            if (!_commandT2C.TryGetValue(evt.GetType(), out var command))
                throw new Exception($"Couldn't find registered command type for type {evt.GetType().FullName}");
            stream.WriteCommand(command);
            evt.Serialize(stream);
        }

        public static string? PromptPassword(string mes)
        {
            Console.Write(mes);

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
        /// Prompt user for SecureString password
        /// </summary>
        /// <param name="mes">Prompt message</param>
        /// <returns>Password or null if terminated</returns>
        public static SecureString? PromptSecureString(string mes)
        {
            Console.Write(mes);

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
    }
}

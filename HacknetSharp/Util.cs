using System;
using System.IO;
using System.Runtime.CompilerServices;
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
        public static void WriteCommand(this Stream stream, ServerClientCommand command) =>
            stream.WriteU32((uint)command);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteCommand(this Stream stream, ClientServerCommand command) =>
            stream.WriteU32((uint)command);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ServerClientCommand ReadServerClientCommand(this Stream stream) =>
            (ServerClientCommand)stream.ReadU32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ClientServerCommand ReadClientServerCommand(this Stream stream) =>
            (ClientServerCommand)stream.ReadU32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Expect(this Stream stream, ServerClientCommand command, out ServerClientCommand actual)
        {
            actual = (ServerClientCommand)stream.ReadU32();
            return actual == command;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Expect(this Stream stream, ClientServerCommand command, out ClientServerCommand actual)
        {
            actual = (ClientServerCommand)stream.ReadU32();
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
    }
}

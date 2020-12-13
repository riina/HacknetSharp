using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents an executable with a <see cref="Run"/> method.
    /// </summary>
    /// <typeparam name="TExecutableContext">Context type.</typeparam>
    public abstract partial class Executable<TExecutableContext> where TExecutableContext : ProcessContext
    {
        /// <summary>
        /// Run this executable with the given context.
        /// </summary>
        /// <param name="context">Context to use with this execution.</param>
        /// <returns>Enumerator that divides execution steps.</returns>
        public abstract IEnumerator<YieldToken?> Run(TExecutableContext context);

        #region Utility methods

        /// <summary>
        /// Gets normalized filesystem path.
        /// </summary>
        /// <param name="path">Input path.</param>
        /// <returns>Normalized path.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetNormalized(string path) => Path.TrimEndingDirectorySeparator(GetFullPath(path, "/"));

        /// <summary>
        /// Gets filename from path.
        /// </summary>
        /// <param name="path">Input path.</param>
        /// <returns>Extracted filename.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetFileName(string path) => Path.GetFileName(path);

        #endregion

        #region Yield tokens

        /// <summary>
        /// Creates a yield token with specified action and yield token. The action is evaluated on first yield.
        /// </summary>
        /// <param name="action">One-off delegate.</param>
        /// <param name="token">Embedded delegate.</param>
        /// <returns>Yield token.</returns>
        public static ActWaitYieldToken ActWait(Action action, YieldToken token) =>
            new ActWaitYieldToken(action, token);

        /// <summary>
        /// Creates a yield token with the specified yield tokens. Once each yield token in turn has yielded, execution is resumed.
        /// </summary>
        /// <param name="tokens">Embedded yield tokens.</param>
        /// <returns>Yield token.</returns>
        public static SequenceYieldToken Sequence(IEnumerable<YieldToken> tokens) => new SequenceYieldToken(tokens);

        /// <summary>
        /// Creates a yield token with the specified yield tokens. Once all yield tokens have yielded, execution is resumed.
        /// </summary>
        /// <param name="tokens">Embedded yield tokens.</param>
        /// <returns>Yield token.</returns>
        public static AggregateYieldToken Aggregate(IEnumerable<YieldToken> tokens) => new AggregateYieldToken(tokens);

        /// <summary>
        /// Creates a yield token with the specified delay. Once the delay has completed (<see cref="DelayYieldToken.Delay"/> &gt; 0), execution is resumed.
        /// </summary>
        /// <param name="delay">Delay in seconds.</param>
        /// <returns>Yield token.</returns>
        public static DelayYieldToken Delay(float delay) => new DelayYieldToken(delay);

        /// <summary>
        /// Creates a yield token with the specified delegate. If the delegate returns true, execution is resumed.
        /// </summary>
        /// <param name="condition">Delegate, triggers execution once it returns true.</param>
        /// <returns>Yield token.</returns>
        public static ConditionYieldToken Condition(Func<bool> condition) => new ConditionYieldToken(condition);

        /// <summary>
        /// Creates a yield token with the specified context and operation. Once a message with the right operation ID is received, execution is resumed.
        /// </summary>
        /// <param name="context">Context to check for messages in.</param>
        /// <param name="hidden">Use hidden input (passwords).</param>
        /// <returns>Yield token.</returns>
        public static InputYieldToken Input(IPersonContext context, bool hidden)
        {
            var opGuid = Guid.NewGuid();
            context.WriteEventSafe(new InputRequestEvent {Operation = opGuid, Hidden = hidden});
            context.FlushSafeAsync();
            return new InputYieldToken(context, opGuid);
        }

        /// <summary>
        /// Creates a yield token with the specified context and operation. Once a message with the right operation ID is received, execution is resumed.
        /// </summary>
        /// <param name="context">Context to check for messages in.</param>
        /// <param name="readOnly">True if only read access is allowed.</param>
        /// <param name="content">Content to edit.</param>
        /// <returns>Yield token.</returns>
        public static EditYieldToken Edit(IPersonContext context, bool readOnly, string content)
        {
            var opGuid = Guid.NewGuid();
            context.WriteEventSafe(new EditRequestEvent {Operation = opGuid, ReadOnly = readOnly, Content = content});
            context.FlushSafeAsync();
            return new EditYieldToken(context, opGuid);
        }

        /// <summary>
        /// Represents a yield token that invokes a delegate only one time.
        /// </summary>
        public class ActWaitYieldToken : YieldToken
        {
            /// <summary>
            /// One-off delegate.
            /// </summary>
            public Action Action { get; }

            /// <summary>
            /// Embedded delegate.
            /// </summary>
            public YieldToken Token { get; }

            private bool _executed;

            /// <summary>
            /// Creates a yield token with specified action and yield token. The action is evaluated on first yield.
            /// </summary>
            /// <param name="action">One-off delegate.</param>
            /// <param name="token">Embedded delegate.</param>
            public ActWaitYieldToken(Action action, YieldToken token)
            {
                Action = action;
                Token = token;
            }

            /// <inheritdoc />
            public override bool Yield(IWorld world)
            {
                if (!_executed)
                    Action();
                _executed = true;
                return Token.Yield(world);
            }
        }

        /// <summary>
        /// Represents a yield token that evaluates embedded yield tokens in sequence.
        /// </summary>
        public class SequenceYieldToken : YieldToken
        {
            /// <summary>
            /// Embedded yield tokens.
            /// </summary>
            public List<YieldToken> Tokens { get; }

            /// <summary>
            /// Creates a yield token with the specified yield tokens. Once each yield token in turn has yielded, execution is resumed.
            /// </summary>
            /// <param name="tokens">Embedded yield tokens.</param>
            public SequenceYieldToken(IEnumerable<YieldToken> tokens)
            {
                Tokens = new List<YieldToken>(tokens);
            }

            /// <inheritdoc />
            public override bool Yield(IWorld world)
            {
                while (Tokens.Count > 0 && Tokens[0].Yield(world))
                    Tokens.RemoveAt(0);
                return Tokens.Count == 0;
            }
        }


        /// <summary>
        /// Represents a yield token that evaluates embedded yield tokens until all have yielded.
        /// </summary>
        public class AggregateYieldToken : YieldToken
        {
            /// <summary>
            /// Embedded yield tokens.
            /// </summary>
            public HashSet<YieldToken> Tokens { get; }

            /// <summary>
            /// Creates a yield token with the specified yield tokens. Once all yield tokens have yielded, execution is resumed.
            /// </summary>
            /// <param name="tokens">Embedded yield tokens.</param>
            public AggregateYieldToken(IEnumerable<YieldToken> tokens)
            {
                Tokens = new HashSet<YieldToken>(tokens);
            }

            /// <inheritdoc />
            public override bool Yield(IWorld world)
            {
                if (Tokens.Count == 0) return true;
                Tokens.RemoveWhere(t => t.Yield(world));
                return Tokens.Count == 0;
            }
        }

        /// <summary>
        /// Represents a delay-based yield token.
        /// </summary>
        public class DelayYieldToken : YieldToken
        {
            /// <summary>
            /// Delay in seconds.
            /// </summary>
            public double Delay { get; set; }

            /// <summary>
            /// Creates a yield token with the specified delay. Once the delay has completed (<see cref="DelayYieldToken.Delay"/> &gt; 0), execution is resumed.
            /// </summary>
            /// <param name="delay">Delay in seconds.</param>
            public DelayYieldToken(double delay)
            {
                Delay = delay;
            }

            /// <inheritdoc />
            public override bool Yield(IWorld world) => (Delay -= world.Time - world.PreviousTime) <= 0.0;
        }

        /// <summary>
        /// Represents a condition-based yield token.
        /// </summary>
        public class ConditionYieldToken : YieldToken
        {
            /// <summary>
            /// Condition for this token.
            /// </summary>
            public Func<bool> Condition { get; }

            /// <summary>
            /// Creates a yield token with the specified delegate. If the delegate returns true, execution is resumed.
            /// </summary>
            /// <param name="condition">Delegate, triggers execution once it returns true.</param>
            public ConditionYieldToken(Func<bool> condition)
            {
                Condition = condition;
            }

            /// <inheritdoc />
            public override bool Yield(IWorld world) => Condition();
        }

        /// <summary>
        /// Represents an input-dependent yield token.
        /// </summary>
        public class InputYieldToken : YieldToken
        {
            /// <summary>
            /// Context.
            /// </summary>
            public IPersonContext Context { get; }

            /// <summary>
            /// Operation ID.
            /// </summary>
            public Guid Operation { get; }

            /// <summary>
            /// Input response.
            /// </summary>
            public InputResponseEvent? Input { get; set; }

            /// <summary>
            /// Creates a yield token with the specified context and operation. Once a message with the right operation ID is received, execution is resumed.
            /// </summary>
            /// <param name="context">Context to check for messages in.</param>
            /// <param name="operation">Operation ID to check for.</param>
            /// <returns>Yield token.</returns>
            public InputYieldToken(IPersonContext context, Guid operation)
            {
                Context = context;
                Operation = operation;
            }

            /// <inheritdoc />
            public override bool Yield(IWorld world)
            {
                if (Input != null) return true;
                if (!Context.Responses.TryRemove(Operation, out var response)) return false;
                Input = response as InputResponseEvent ?? new InputResponseEvent {Operation = Operation, Input = ""};
                return true;
            }
        }

        /// <summary>
        /// Represents an edit-dependent yield token.
        /// </summary>
        public class EditYieldToken : YieldToken
        {
            /// <summary>
            /// Context.
            /// </summary>
            public IPersonContext Context { get; }

            /// <summary>
            /// Operation ID.
            /// </summary>
            public Guid Operation { get; }

            /// <summary>
            /// Edit response.
            /// </summary>
            public EditResponseEvent? Edit { get; set; }

            /// <summary>
            /// Creates a yield token with the specified context and operation. Once a message with the right operation ID is received, execution is resumed.
            /// </summary>
            /// <param name="context">Context to check for messages in.</param>
            /// <param name="operation">Operation ID to check for.</param>
            /// <returns>Yield token.</returns>
            public EditYieldToken(IPersonContext context, Guid operation)
            {
                Context = context;
                Operation = operation;
            }

            /// <inheritdoc />
            public override bool Yield(IWorld world)
            {
                if (Edit != null) return true;
                if (!Context.Responses.TryRemove(Operation, out var response)) return false;
                Edit = response as EditResponseEvent ??
                       new EditResponseEvent {Operation = Operation, Content = "", Write = false};
                return true;
            }
        }

        #endregion
    }
}

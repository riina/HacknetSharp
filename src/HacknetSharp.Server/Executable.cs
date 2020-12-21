using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using HacknetSharp.Events.Client;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents the base type for executables, with related convenience methods.
    /// </summary>
    public abstract partial class Executable
    {
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

        /// <summary>
        /// Gets deepest common path between two paths.
        /// </summary>
        /// <param name="left">First path.</param>
        /// <param name="right">Second path.</param>
        /// <returns>Deepest common path.</returns>
        /// <remarks>Examples:
        /// <br/>
        /// "/path/d/something", "/path/d/something" -> "/path/d"
        /// "/path/d/something", "/path/drawl/something" -> "/path"
        /// "/left/something", "/right/something" -> "/"
        /// </remarks>
        public static string GetPathInCommon(string left, string right)
        {
            // Normalized guarantees at least len 1 where both start with /
            left = GetNormalized(left);
            right = GetNormalized(right);
            if (left == right) return left;
            int min = Math.Min(left.Length, right.Length);
            int prevLenCommonChar = 1;
            int lenCommonChar = 1;
            if (right.Length < left.Length)
            {
                string tmp = right;
                right = left;
                left = tmp;
            }

            while (lenCommonChar < min && left[lenCommonChar] == right[lenCommonChar])
            {
                if (IsSplit(left, lenCommonChar))
                    prevLenCommonChar = lenCommonChar;
                lenCommonChar++;
            }

            return left.Length != right.Length && IsSplit(right, lenCommonChar)
                ? right[..lenCommonChar]
                : left[..prevLenCommonChar];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSplit(ReadOnlySpan<char> span, int idx) =>
            span[idx] == '/' && (idx == 0 || span[idx - 1] != '\\');

        /// <summary>
        /// Splits a path into its directory and filename.
        /// </summary>
        /// <param name="path">Path to split.</param>
        /// <param name="normalize">If true, apply normalization to output.</param>
        /// <returns>Tuple containing directory and filename.</returns>
        public static (string, string) GetDirectoryAndName(string path, bool normalize = true)
        {
            string dir = GetDirectoryName(path) ?? "/";
            return (normalize ? GetNormalized(dir) : dir, GetFileName(path));
        }

        /// <summary>
        /// Attempt to write log to /log directory.
        /// </summary>
        /// <param name="spawn">Active spawn manager.</param>
        /// <param name="time">Log timestamp.</param>
        /// <param name="system">Active system.</param>
        /// <param name="login">Active login.</param>
        /// <param name="logKind">Log kind.</param>
        /// <param name="logBody">Log body.</param>
        /// <param name="log">Generated log.</param>
        /// <returns>True if successful.</returns>
        public static bool TryWriteLog(WorldSpawn spawn, double time, SystemModel system, LoginModel login,
            string logKind, string logBody, [NotNullWhen(true)] out FileModel? log)
        {
            if (system.GetFileSystemEntry("/log") == null)
            {
                // /log does not exist, make it
                var logDir = spawn.Folder(system, login, "log", "/");
                logDir.Read = FileModel.AccessLevel.Everyone;
                logDir.Write = FileModel.AccessLevel.Everyone;
                logDir.Execute = FileModel.AccessLevel.Everyone;
            }

            string fn = $"{ServerUtil.GetHexTimestamp(time)}_{logKind}_{login.User}.log";
            system.TryGetFile($"/log/{fn}", login, out var accessResult, out _, out var readable);
            if (accessResult != ReadAccessResult.NoExist ||
                readable == null || readable.FullPath != "/log" || readable.Kind != FileModel.FileKind.Folder)
            {
                log = null;
                return false;
            }

            try
            {
                log = spawn.TextFile(system, login, fn, "/log", logBody);
                log.Read = FileModel.AccessLevel.Everyone;
                log.Write = FileModel.AccessLevel.Owner;
                log.Execute = FileModel.AccessLevel.Everyone;
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected failure in {nameof(Executable)}{nameof(TryWriteLog)}:\n{e}");
                log = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to find a system on the network with the specified address.
        /// </summary>
        /// <param name="worldModel">World model to search.</param>
        /// <param name="addr">Target address.</param>
        /// <param name="system">Found system.</param>
        /// <param name="errorMessage">Error message if failed.</param>
        /// <returns>True if matching system was found.</returns>
        public static bool TryGetSystem(WorldModel worldModel, string addr,
            [NotNullWhen(true)] out SystemModel? system,
            [NotNullWhen(false)] out string? errorMessage)
        {
            if (!IPAddressRange.TryParse(addr, false, out var ip) ||
                !ip.TryGetIPv4HostAndSubnetMask(out uint host, out _))
            {
                errorMessage = "Invalid address format";
                system = null;
                return false;
            }

            system = worldModel.Systems.FirstOrDefault(s => s.Address == host);
            if (system == null)
            {
                errorMessage = "No route to host";
                return false;
            }

            errorMessage = null;
            return true;
        }

        #endregion

        #region Yield tokens

        /// <summary>
        /// Creates a yield token with specified action and yield token. The action is evaluated on first yield.
        /// </summary>
        /// <param name="action">One-off delegate.</param>
        /// <param name="token">Embedded delegate.</param>
        /// <returns>Yield token.</returns>
        public static ActWaitYieldToken ActWait(Action action, YieldToken token) =>
            new(action, token);

        /// <summary>
        /// Creates a yield token with the specified yield tokens. Once each yield token in turn is done yielding, execution is resumed.
        /// </summary>
        /// <param name="tokens">Embedded yield tokens.</param>
        /// <returns>Yield token.</returns>
        public static SequenceYieldToken Sequence(IEnumerable<YieldToken> tokens) => new(tokens);

        /// <summary>
        /// Creates a yield token with the specified yield tokens. Once all yield tokens are done yielding, execution is resumed.
        /// </summary>
        /// <param name="tokens">Embedded yield tokens.</param>
        /// <returns>Yield token.</returns>
        public static AggregateYieldToken Aggregate(IEnumerable<YieldToken> tokens) => new(tokens);

        /// <summary>
        /// Creates a yield token with the specified yield tokens. Once any yield token is done yielding, execution is resumed.
        /// </summary>
        /// <param name="tokens">Embedded yield tokens.</param>
        /// <returns>Yield token.</returns>
        public static AnyYieldToken Any(IEnumerable<YieldToken> tokens) => new(tokens);

        /// <summary>
        /// Creates a yield token with the specified delay. Once the delay has completed (<see cref="DelayYieldToken.Delay"/> &gt; 0), execution is resumed.
        /// </summary>
        /// <param name="delay">Delay in seconds.</param>
        /// <returns>Yield token.</returns>
        public static DelayYieldToken Delay(double delay) => new(delay);

        /// <summary>
        /// Creates a yield token with the specified delegate. If the delegate returns true, execution is resumed.
        /// </summary>
        /// <param name="condition">Delegate, triggers execution once it returns true.</param>
        /// <returns>Yield token.</returns>
        public static ConditionYieldToken Condition(Func<bool> condition) => new(condition);

        /// <summary>
        /// Sends an input request and creates a yield token with the specified context and operation. Once a message with the right operation ID is received, execution is resumed.
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
        /// Sends an input request and creates a yield token with the specified context and operation.Once a message with the right operation ID is received, execution is resumed.
        /// </summary>
        /// <param name="context">Context to check for messages in.</param>
        /// <param name="hidden">Use hidden input (passwords).</param>
        /// <param name="confirmSet">Confirmation set.</param>
        /// <returns>Yield token.</returns>
        public static ConfirmYieldToken Confirm(IPersonContext context, bool hidden,
            IReadOnlyCollection<string>? confirmSet = null)
        {
            var opGuid = Guid.NewGuid();
            context.WriteEventSafe(new InputRequestEvent {Operation = opGuid, Hidden = hidden});
            context.FlushSafeAsync();
            confirmSet ??= Util.YesAnswers;
            return new ConfirmYieldToken(context, opGuid, confirmSet);
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
            /// Creates a yield token with the specified yield tokens. Once each yield token in turn is done yielding, execution is resumed.
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
        /// Represents a yield token that evaluates embedded yield tokens until all are done yielding.
        /// </summary>
        public class AggregateYieldToken : YieldToken
        {
            /// <summary>
            /// Embedded yield tokens.
            /// </summary>
            public HashSet<YieldToken> Tokens { get; }

            /// <summary>
            /// Creates a yield token with the specified yield tokens. Once all yield tokens are done yielding, execution is resumed.
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
        /// Represents a yield token that evaluates embedded yield tokens until one is done yielding.
        /// </summary>
        public class AnyYieldToken : YieldToken
        {
            /// <summary>
            /// Embedded yield tokens.
            /// </summary>
            public HashSet<YieldToken> Tokens { get; }

            /// <summary>
            /// Creates a yield token with the specified yield tokens. Once one is done yielding, execution is resumed.
            /// </summary>
            /// <param name="tokens">Embedded yield tokens.</param>
            public AnyYieldToken(IEnumerable<YieldToken> tokens)
            {
                Tokens = new HashSet<YieldToken>(tokens);
            }

            /// <inheritdoc />
            public override bool Yield(IWorld world)
            {
                if (Tokens.Count == 0) return true;
                if (Tokens.Any(t => t.Yield(world)))
                    Tokens.Clear();
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

            private bool _condition;

            /// <summary>
            /// Creates a yield token with the specified delay. Once the delay has completed (<see cref="DelayYieldToken.Delay"/> &gt; 0), execution is resumed.
            /// </summary>
            /// <param name="delay">Delay in seconds.</param>
            public DelayYieldToken(double delay)
            {
                Delay = delay;
            }

            /// <inheritdoc />
            public override bool Yield(IWorld world) =>
                _condition || (_condition = (Delay -= world.Time - world.PreviousTime) <= 0.0);
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

            private bool _condition;

            /// <summary>
            /// Creates a yield token with the specified delegate. If the delegate returns true, execution is resumed.
            /// </summary>
            /// <param name="condition">Delegate, triggers execution once it returns true.</param>
            public ConditionYieldToken(Func<bool> condition)
            {
                Condition = condition;
            }

            /// <inheritdoc />
            public override bool Yield(IWorld world) => _condition || (_condition = Condition());
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
        /// Represents a confirmation-dependent yield token.
        /// </summary>
        public class ConfirmYieldToken : InputYieldToken
        {
            /// <summary>
            /// Set of confirmation keys.
            /// </summary>
            public IReadOnlyCollection<string> ConfirmSet { get; }

            /// <summary>
            /// If true, user confirmed action.
            /// </summary>
            public bool Confirmed { get; set; }

            /// <summary>
            /// Creates a yield token with the specified context and operation. Once a message with the right operation ID is received, execution is resumed.
            /// </summary>
            /// <param name="context">Context to check for messages in.</param>
            /// <param name="operation">Operation ID to check for.</param>
            /// <param name="confirmSet">Set of confirmation keys.</param>
            /// <returns>Yield token.</returns>
            public ConfirmYieldToken(IPersonContext context, Guid operation, IReadOnlyCollection<string> confirmSet) :
                base(context, operation)
            {
                ConfirmSet = confirmSet;
            }

            /// <inheritdoc />
            public override bool Yield(IWorld world)
            {
                if (Input != null) return true;
                bool res = base.Yield(world);
                if (res) Confirmed = ConfirmSet.Contains(Input!.Input, StringComparer.InvariantCultureIgnoreCase);
                return res;
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

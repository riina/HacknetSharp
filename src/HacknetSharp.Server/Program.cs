using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.CorePrograms;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents an executable that depends on a shell and related state.
    /// </summary>
    public abstract class Program : Executable
    {
        /// <summary>
        /// Execution context.
        /// </summary>
        public ProgramContext Context { get; set; } = null!;

        /// <summary>
        /// Person for the process.
        /// </summary>
        public PersonModel Person => Context.Person;

        /// <summary>
        /// User/NPC context for the process.
        /// </summary>
        public IPersonContext User => Context.User;

        /// <summary>
        /// Shell for the process.
        /// </summary>
        public ShellProcess Shell => Context.Shell;

        /// <summary>
        /// Operation ID for the process.
        /// </summary>
        public Guid OperationId => Context.OperationId;

        /// <summary>
        /// Invocation type for the process.
        /// </summary>
        public ProgramContext.InvocationType Type => Context.Type;

        /// <summary>
        /// Console width the process was called with.
        /// </summary>
        public int ConWidth => Context.ConWidth;

        /// <summary>
        /// True if the context is for an AI character.
        /// </summary>
        public bool IsAi => Context.IsAi;

        /// <summary>
        /// Optional post-execution command to execute.
        /// </summary>
        public string[]? ChainLine
        {
            get => Context.ChainLine;
            set => Context.ChainLine = value;
        }

        /// <summary>
        /// Remote shell this process is connected to.
        /// </summary>
        public ShellProcess? Remote
        {
            get => Context.Remote;
            set => Context.Remote = value;
        }

        #region Utility methods

        /// <summary>
        /// Write content to pseudo-terminal output.
        /// </summary>
        /// <param name="evt">Event to write.</param>
        /// <returns>This object (for chaining).</returns>
        public Program WriteEvent(ServerEvent evt)
        {
            User.WriteEventSafe(evt);
            return this;
        }

        /// <summary>
        /// Write content to pseudo-terminal output.
        /// </summary>
        /// <param name="obj">Object to write.</param>
        /// <returns>This object (for chaining).</returns>
        public Program Write(object obj)
        {
            User.WriteEventSafe(Output(obj is string str ? str : obj.ToString() ?? "<object>"));
            return this;
        }

        /// <summary>
        /// Flush output buffer to user's stream.
        /// </summary>
        /// <returns>This object (for chaining).</returns>
        public Program Flush()
        {
            User.FlushSafeAsync();
            return this;
        }

        /// <summary>
        /// Sends an input request and creates a yield token with the specified context and operation. Once a message with the right operation ID is received, execution is resumed.
        /// </summary>
        /// <param name="hidden">Use hidden input (passwords).</param>
        /// <returns>Yield token.</returns>
        public InputYieldToken Input(bool hidden) => Input(User, hidden);

        /// <summary>
        /// Sends an input request and creates a yield token with the specified context and operation.Once a message with the right operation ID is received, execution is resumed.
        /// </summary>
        /// <param name="hidden">Use hidden input (passwords).</param>
        /// <param name="confirmSet">Confirmation set.</param>
        /// <returns>Yield token.</returns>
        public ConfirmYieldToken Confirm(bool hidden,
            IReadOnlyCollection<string>? confirmSet = null) => Confirm(User, hidden, confirmSet);

        /// <summary>
        /// Creates a yield token with the specified context and operation. Once a message with the right operation ID is received, execution is resumed.
        /// </summary>
        /// <param name="readOnly">True if only read access is allowed.</param>
        /// <param name="content">Content to edit.</param>
        /// <returns>Yield token.</returns>
        public EditYieldToken Edit(bool readOnly, string content) => Edit(User, readOnly, content);

        /// <summary>
        /// Attempt to connect to target system with credentials.
        /// </summary>
        /// <param name="address">Target system.</param>
        /// <param name="username">Target username.</param>
        /// <param name="password">Target password.</param>
        /// <returns>True on successful connection.</returns>
        public bool Connect(uint address, string username, string password) =>
            Connect(Context, address, username, password);

        /// <summary>
        /// Attempt to connect to target system with credentials.
        /// </summary>
        /// <param name="context">Program context.</param>
        /// <param name="address">Target system.</param>
        /// <param name="username">Target username.</param>
        /// <param name="password">Target password.</param>
        /// <returns>True on successful connection.</returns>
        public static bool Connect(ProgramContext context, uint address, string username, string password)
        {
            var user = context.User;
            user.WriteEventSafe(Output("Connecting...\n"));
            if (!context.World.Model.AddressedSystems.TryGetValue(address, out var system))
            {
                user.WriteEventSafe(Output("No route to host\n"));
                user.FlushSafeAsync();
                return false;
            }

            var login = system.Logins.FirstOrDefault(l => l.User == username);
            if (login == null || !ServerUtil.ValidatePassword(password, login.Hash, login.Salt))
            {
                user.WriteEventSafe(Output("Invalid credentials\n"));
                user.FlushSafeAsync();
                return false;
            }

            context.World.StartShell(user, context.Person, login, new[] { ServerConstants.ShellName }, true);
            if (context.System.KnownSystems.All(p => p.To != system))
                context.World.Spawn.Connection(context.System, system, false);
            if (system.ConnectCommandLine != null)
            {
                string[] chainLine = system.ConnectCommandLine.SplitCommandLine();
                if (chainLine.Length != 0 && !string.IsNullOrWhiteSpace(chainLine[0]))
                    context.ChainLine = chainLine;
            }

            user.FlushSafeAsync();
            return true;
        }

        /// <summary>
        /// Creates an <see cref="OutputEvent"/> with the specified message.
        /// </summary>
        /// <param name="message">Message to use.</param>
        /// <returns>Event with provided message.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OutputEvent Output(string message) => new() { Text = message };

        /// <summary>
        /// Tries to find a system on the network with the specified address.
        /// </summary>
        /// <param name="addr">Target address.</param>
        /// <param name="system">Found system.</param>
        /// <param name="errorMessage">Error message if failed.</param>
        /// <returns>True if matching system was found.</returns>
        public bool TryGetSystem(string addr,
            [NotNullWhen(true)] out SystemModel? system,
            [NotNullWhen(false)] out string? errorMessage) =>
            TryGetSystem(World.Model, addr, out system, out errorMessage);

        /// <summary>
        /// Tries to find a system on the network with the specified address.
        /// </summary>
        /// <param name="host">Target address.</param>
        /// <param name="system">Found system.</param>
        /// <param name="errorMessage">Error message if failed.</param>
        /// <returns>True if matching system was found.</returns>
        public bool TryGetSystem(uint host,
            [NotNullWhen(true)] out SystemModel? system,
            [NotNullWhen(false)] out string? errorMessage) =>
            TryGetSystem(World.Model, host, out system, out errorMessage);

        /// <summary>
        /// Try to obtain a value with a shell variable as backup.
        /// </summary>
        /// <param name="value">Known value (passed through if not null).</param>
        /// <param name="shellVariable">Shell variable to check.</param>
        /// <param name="result">Known value from passed or shell variables.</param>
        /// <returns>True if value is known.</returns>
        public bool TryGetVariable(string? value, string shellVariable,
            [NotNullWhen(true)] out string? result) => TryGetVariable(Context, value, shellVariable, out result);

        /// <summary>
        /// Try to obtain a value with a shell variable as backup.
        /// </summary>
        /// <param name="context">Program context.</param>
        /// <param name="value">Known value (passed through if not null).</param>
        /// <param name="shellVariable">Shell variable to check.</param>
        /// <param name="result">Known value from passed or shell variables.</param>
        /// <returns>True if value is known.</returns>
        public static bool TryGetVariable(ProgramContext context, string? value, string shellVariable,
            [NotNullWhen(true)] out string? result)
        {
            if (value != null)
            {
                result = value;
                return true;
            }

            if (context.Shell.TryGetVariable(shellVariable, out string? variableResult))
            {
                result = variableResult;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Sends an <see cref="OperationCompleteEvent"/> to the client to allow command entry.
        /// </summary>
        public void SignalUnbindProcess() => SignalUnbindProcess(Context, null);

        /// <summary>
        /// Sends an <see cref="OperationCompleteEvent"/> to the client to allow command entry.
        /// </summary>
        /// <param name="programContext">Context to use.</param>
        /// <param name="process">Associated process (used to check <see cref="Process.Completed"/>).</param>
        public static void SignalUnbindProcess(ProgramContext programContext, Process? process)
        {
            try
            {
                // just ignore shells
                if (process is ShellProcess || process?.Executable is ShellProxyProgram) return;
                if (programContext.ChainLine != null &&
                    (process?.Completed ?? Process.CompletionKind.Normal) == Process.CompletionKind.Normal)
                    return;
                var chain = programContext.Person.ShellChain;
                if (chain.Count == 0) return;
                var topShell = chain[^1];
                // Shell is popped before signalled, so check if we're either in the top shell or our shell has been popped
                if (topShell != programContext.Shell && chain.Contains(programContext.Shell)) return;
                programContext.User.WriteEventSafe(ServerUtil.CreatePromptEvent(topShell));
            }
            finally
            {
                programContext.User.WriteEventSafe(new OperationCompleteEvent { Operation = programContext.OperationId });
                programContext.User.FlushSafeAsync();
            }
        }

        #endregion

        /// <summary>
        /// Isolate flags from argv.
        /// </summary>
        /// <param name="argv">Argv to sort.</param>
        /// <param name="optKeys">Option keys.</param>
        /// <returns>Flags, options, and arguments.</returns>
        public static (HashSet<string> flags, Dictionary<string, string> opts, List<string> args) IsolateArgvFlags(
            string[] argv, IReadOnlySet<string>? optKeys = null) =>
            ServerUtil.IsolateFlags(new ArraySegment<string>(argv, 1, argv.Length - 1), optKeys);
    }
}

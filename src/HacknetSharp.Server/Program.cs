using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HacknetSharp.Events.Server;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents an executable that depends on a shell and related state.
    /// </summary>
    public abstract class Program : Executable
    {
        /// <summary>
        /// Checks memory that will be used if this context executes.
        /// </summary>
        /// <param name="context">Program context to operate with.</param>
        /// <returns>Memory to be initially allocated by program.</returns>
        public virtual long GetStartupMemory(ProgramContext context) => 0;

        /// <summary>
        /// Run this executable with the given context.
        /// </summary>
        /// <param name="context">Context to use with this execution.</param>
        /// <returns>Enumerator that divides execution steps.</returns>
        public abstract IEnumerator<YieldToken?> Run(ProgramContext context);

        /// <summary>
        /// Tells program on given context to stop execution.
        /// </summary>
        /// <param name="context">Program context to operate with.</param>
        /// <returns>False if service refuses to shutdown.</returns>
        public virtual bool OnShutdown(ProgramContext context) => true;

        #region Utility methods

        /// <summary>
        /// Creates an <see cref="OutputEvent"/> with the specified message.
        /// </summary>
        /// <param name="message">Message to use.</param>
        /// <returns>Event with provided message.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OutputEvent Output(string message) => new() {Text = message};

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
        /// Sends a <see cref="OperationCompleteEvent"/> to the client to allow command entry.
        /// </summary>
        /// <param name="programContext">Context to use.</param>
        /// <param name="process">Associated process (used to check <see cref="Process.Completed"/>).</param>
        public static void SignalUnbindProcess(ProgramContext programContext, Process? process)
        {
            try
            {
                // just ignore shells
                if (process is ShellProcess) return;
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
                programContext.User.WriteEventSafe(new OperationCompleteEvent {Operation = programContext.OperationId});
                programContext.User.FlushSafeAsync();
            }
        }

        #endregion
    }
}

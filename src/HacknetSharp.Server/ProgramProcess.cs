using System;
using System.Collections.Generic;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a running program.
    /// </summary>
    public class ProgramProcess : Process
    {
        /// <summary>
        /// Program context associated with this process.
        /// </summary>
        public ProgramContext ProgramContext { get; set; }

        private readonly IEnumerator<YieldToken?> _enumerator;
        private readonly Func<ProgramContext, bool> _shutdownCallback;
        private YieldToken? _currentToken;
        private bool _cleaned;

        /// <summary>
        /// Creates a new instance of <see cref="ProgramProcess"/>.
        /// </summary>
        /// <param name="context">Program context.</param>
        /// <param name="program">Program this process will use.</param>
        public ProgramProcess(ProgramContext context, Program program) : base(context, program)
        {
            ProgramContext = context;
            string[] argv = context.Argv;
            var env = new Dictionary<string, string>(context.Shell.GetVariables());
            int count = argv.Length;
            for (int i = 0; i < count; i++)
                argv[i] = argv[i].ApplyShellReplacements(env);
            _enumerator = program.Run(context);
            _shutdownCallback = program.OnShutdown;
        }

        /// <inheritdoc />
        public override bool Update(IWorld world)
        {
            if (!ProgramContext.User.Connected) return true;
            if (_currentToken != null)
                if (!_currentToken.Yield(world)) return false;
                else _currentToken = null;

            if (!_enumerator.MoveNext())
            {
                world.CompleteRecurse(this, CompletionKind.Normal);
                return true;
            }

            _currentToken = _enumerator.Current;
            return false;
        }

        /// <inheritdoc />
        public override bool Complete(CompletionKind completionKind)
        {
            if (_cleaned) return true;
            if (!_shutdownCallback(ProgramContext)) return false;
            _cleaned = true;
            Completed = completionKind;

            if (ProgramContext.IsAi) return true;

            if (!ProgramContext.User.Connected) return true;

            if (ProgramContext.Type == ProgramContext.InvocationType.StartUp) ProgramContext.Person.StartedUp = true;
            if (completionKind != CompletionKind.Normal)
            {
                ProgramContext.User.WriteEventSafe(
                    Program.Output($"[Process {ProgramContext.Pid} {ProgramContext.Argv[0]} terminated]\n"));
                ProgramContext.User.FlushSafeAsync();
            }

            string[]? chainLine = ProgramContext.ChainLine;
            if (ProgramContext.Type == ProgramContext.InvocationType.StartUp &&
                ProgramContext.System.ConnectCommandLine != null)
                chainLine ??= ServerUtil.SplitCommandLine(ProgramContext.System.ConnectCommandLine);
            if (chainLine != null && chainLine.Length != 0 && !string.IsNullOrWhiteSpace(chainLine[0]))
            {
                ProgramContext.ChainLine = chainLine;
                return true;
            }

            Program.SignalUnbindProcess(ProgramContext, this);
            return true;
        }
    }
}

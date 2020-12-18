using System.Collections.Generic;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a running program.
    /// </summary>
    public class ProgramProcess : Process
    {
        private readonly ProgramContext _programContext;
        private readonly IEnumerator<YieldToken?> _enumerator;
        private YieldToken? _currentToken;
        private bool _cleaned;

        /// <summary>
        /// Creates a new instance of <see cref="ProgramProcess"/>.
        /// </summary>
        /// <param name="context">Program context.</param>
        /// <param name="program">Program this process will use.</param>
        public ProgramProcess(ProgramContext context, Program program) : base(context)
        {
            _programContext = context;
            string[] argv = context.Argv;
            var env = new Dictionary<string, string>(context.Shell.GetVariables());
            int count = argv.Length;
            for (int i = 0; i < count; i++)
                argv[i] = argv[i].ApplyShellReplacements(env);
            _enumerator = program.Run(context);
        }

        /// <inheritdoc />
        public override bool Update(IWorld world)
        {
            if (!_programContext.User.Connected) return true;
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
            _cleaned = true;
            Completed = completionKind;

            if (_programContext.IsAi) return true;

            if (!_programContext.User.Connected) return true;

            if (_programContext.Type == ProgramContext.InvocationType.StartUp) _programContext.Person.StartedUp = true;

            if (completionKind != CompletionKind.Normal)
            {
                _programContext.User.WriteEventSafe(
                    Program.Output($"[Process {_programContext.Pid} {_programContext.Argv[0]} terminated]\n"));
                _programContext.User.FlushSafeAsync();
            }

            string[]? chainLine = _programContext.ChainLine;
            if (_programContext.Type == ProgramContext.InvocationType.StartUp &&
                _programContext.System.ConnectCommandLine != null)
                chainLine ??= ServerUtil.SplitCommandLine(_programContext.System.ConnectCommandLine);
            if (chainLine != null && chainLine.Length != 0 && !string.IsNullOrWhiteSpace(chainLine[0]))
            {
                _programContext.ChainLine = chainLine;
                return true;
            }

            Program.SignalUnbindProcess(_programContext, this);
            return true;
        }
    }
}

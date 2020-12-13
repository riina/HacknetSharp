using System.Collections.Generic;

namespace HacknetSharp.Server
{
    public class ProgramProcess : Process
    {
        private ProgramContext _programContext;
        private readonly IEnumerator<YieldToken?> _enumerator;
        private YieldToken? _currentToken;
        private bool _cleaned;

        public ProgramProcess(ProgramContext context, Program program) : base(context)
        {
            _programContext = context;
            var argv = context.Argv;
            var env = context.Shell.Variables;
            int count = argv.Length;
            for (int i = 0; i < count; i++)
                argv[i] = argv[i].ApplyShellReplacements(env);
            _enumerator = program.Run(context);
        }

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

        public override void Complete(CompletionKind completionKind)
        {
            if (_cleaned) return;
            _cleaned = true;
            Completed = completionKind;

            if (_programContext.IsAI) return;

            if (!_programContext.User.Connected) return;

            if (_programContext.Type == ProgramContext.InvocationType.StartUp) _programContext.Person.StartedUp = true;

            if (completionKind != CompletionKind.Normal)
            {
                _programContext.User.WriteEventSafe(
                    Program.Output($"[Process {_programContext.Pid} {_programContext.Argv[0]} terminated]\n"));
                _programContext.User.FlushSafeAsync();
            }

            var chainLine = _programContext.ChainLine;
            if (_programContext.Type == ProgramContext.InvocationType.StartUp &&
                _programContext.System.ConnectCommandLine != null)
                chainLine ??= Arguments.SplitCommandLine(_programContext.System.ConnectCommandLine);
            if (chainLine != null && chainLine.Length != 0 && !string.IsNullOrWhiteSpace(chainLine[0]))
            {
                _programContext.ChainLine = chainLine;
                return;
            }

            ServerUtil.SignalUnbindProcess(_programContext, this);
        }
    }
}

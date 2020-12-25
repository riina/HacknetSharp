using System.Collections.Generic;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a running program.
    /// </summary>
    public class ProgramProcess : Process
    {
        /// <summary>
        /// Context associated with this process.
        /// </summary>
        public ProgramContext ProgramContext { get; }

        private readonly Program _program;
        private IEnumerator<YieldToken?>? _enumerator;
        private YieldToken? _currentToken;
        private bool _cleaned;

        /// <summary>
        /// Creates a new instance of <see cref="ProgramProcess"/>.
        /// </summary>
        /// <param name="program">Program this process will use.</param>
        public ProgramProcess(Program program) : base(program)
        {
            _program = program;
            ProgramContext = program.Context;
            string[] argv = ProgramContext.Argv;
            var env = new Dictionary<string, string>(ProgramContext.Shell.GetVariables());
            int count = argv.Length;
            for (int i = 0; i < count; i++)
                argv[i] = argv[i].ApplyShellReplacements(env);
        }

        /// <inheritdoc />
        public override bool Update(IWorld world)
        {
            _enumerator ??= Executable.Run();
            if (!ProgramContext.User.Connected) return true;
            if (_currentToken != null)
                if (!_currentToken.Yield(world)) return false;
                else _currentToken = null;

            if (!_enumerator.MoveNext())
            {
                _program.Flush();
                world.CompleteRecurse(this, CompletionKind.Normal);
                return true;
            }

            _program.Flush();
            _currentToken = _enumerator.Current;
            return false;
        }

        /// <inheritdoc />
        public override bool Complete(CompletionKind completionKind)
        {
            if (_cleaned) return true;
            if (!Executable.OnShutdown()) return false;
            _cleaned = true;
            Completed = completionKind;

            if (!ProgramContext.User.Connected) return true;

            if (ProgramContext.Type == ProgramContext.InvocationType.StartUp) ProgramContext.Person.StartedUp = true;
            if (completionKind != CompletionKind.Normal && !ProgramContext.IsAi)
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

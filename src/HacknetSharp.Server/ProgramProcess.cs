﻿using System.Collections.Generic;
using HacknetSharp.Events.Server;

namespace HacknetSharp.Server
{
    public class ProgramProcess : Process
    {
        private readonly ProgramContext _context;
        private readonly IEnumerator<YieldToken?> _enumerator;
        private YieldToken? _currentToken;
        private bool _cleaned;

        public ProgramProcess(ProgramContext context, Program program) : base(context)
        {
            _context = context;
            _enumerator = program.Run(context);
        }

        public override bool Update(IWorld world)
        {
            if (!_context.User.Connected) return true;
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
            if (_context.IsAI)
                return;

            if (!_context.User.Connected) return;

            if (_context.Type == ProgramContext.InvocationType.StartUp)
                _context.Person.StartedUp = true;

            if (completionKind != CompletionKind.Normal)
            {
                _context.User.WriteEventSafe(Program.Output("[Process terminated]"));
                _context.User.FlushSafeAsync();
            }

            var shellChain = _context.Person.ShellChain;
            uint addr = 0;
            if (shellChain.Count != 0)
                addr = shellChain[^1].ProgramContext.System.Address;
            string path = _context.Shell.WorkingDirectory;
            var chainLine = _context.ChainLine;
            if (_context.Type == ProgramContext.InvocationType.StartUp && _context.System.ConnectCommandLine != null)
                chainLine ??= Arguments.SplitCommandLine(_context.System.ConnectCommandLine);
            if (chainLine != null && chainLine.Length != 0 && !string.IsNullOrWhiteSpace(chainLine[0]))
            {
                _context.ChainLine = chainLine;
                return;
            }

            _context.User.WriteEventSafe(new OperationCompleteEvent
            {
                Operation = _context.OperationId, Address = addr, Path = path
            });
            _context.User.FlushSafeAsync();
        }
    }
}

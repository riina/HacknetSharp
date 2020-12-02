using System;
using System.Collections.Generic;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server
{
    public class ProgramOperation : ExecutableOperation
    {
        private readonly ProgramContext _context;
        private readonly IEnumerator<YieldToken?> _enumerator;
        private YieldToken? _currentToken;
        private bool _cleaned;

        public ProgramOperation(ProgramContext context, Program program)
        {
            _context = context;
            _enumerator = program.Run(context);
        }

        public override bool Update(IWorld world)
        {
            if (_currentToken != null)
                if (!_currentToken.Yield(world)) return false;
                else _currentToken = null;

            if (!_enumerator.MoveNext()) return true;
            _currentToken = _enumerator.Current;
            return false;
        }

        public override void Complete()
        {
            if (_cleaned) return;
            _cleaned = true;
            if (_context.Type == ProgramContext.InvocationType.StartUp)
                _context.Person.StartedUp = true;
            if (!_context.User.Connected) return;
            uint addr = _context.System.Model.Address;
            string path = _context.Person.WorkingDirectory;
            switch (_context.Type)
            {
                case ProgramContext.InvocationType.Standard:
                    _context.User.WriteEventSafe(new OperationCompleteEvent
                    {
                        Operation = _context.OperationId, Address = addr, Path = path
                    });
                    break;
                case ProgramContext.InvocationType.Connect:
                    _context.User.WriteEventSafe(new InitialCommandCompleteEvent
                    {
                        Operation = _context.OperationId, Address = addr, Path = path, NeedsRetry = false
                    });
                    break;
                case ProgramContext.InvocationType.StartUp:
                    _context.User.WriteEventSafe(new InitialCommandCompleteEvent
                    {
                        Operation = _context.OperationId, Address = addr, Path = path, NeedsRetry = true
                    });
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_context.Disconnect)
                _context.User.WriteEventSafe(new ServerDisconnectEvent {Reason = "Disconnected by server."});
            else if(_context.Type != ProgramContext.InvocationType.StartUp)
                _context.User.WriteEventSafe(World.CreatePromptEvent(_context.System.Model, _context.Person));
            _context.User.FlushSafeAsync();
        }
    }
}

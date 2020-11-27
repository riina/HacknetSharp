using System;
using System.Collections.Generic;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server
{
    public class ProgramOperation
    {
        public CommandContext Context { get; }
        public Guid OperationId { get; }
        private readonly IEnumerator<Program.YieldToken?> _enumerator;
        private Program.YieldToken? _currentToken;

        public ProgramOperation(CommandContext context, IEnumerator<Program.YieldToken?> enumerator)
        {
            Context = context;
            _enumerator = enumerator;
        }

        /// <summary>
        /// Update current operation.
        /// </summary>
        /// <returns>True when operation is complete.</returns>
        public bool Update(IWorld world)
        {
            if (_currentToken != null)
                if (!_currentToken.Yield(world)) return false;
                else _currentToken = null;

            if (!_enumerator.MoveNext()) return true;
            _currentToken = _enumerator.Current;
            return false;
        }
    }
}

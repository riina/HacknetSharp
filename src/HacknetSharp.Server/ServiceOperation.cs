using System.Collections.Generic;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server
{
    public class ServiceOperation : ExecutableOperation
    {
        private readonly ServiceContext _context;
        private readonly IEnumerator<YieldToken?> _enumerator;
        private YieldToken? _currentToken;
        private bool _cleaned;

        public ServiceOperation(ServiceContext context, Service service)
        {
            _context = context;
            _enumerator = service.Run(context);
        }

        public override bool Update(IWorld world)
        {
            // TODO check system is still valid and is up
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
        }
    }
}

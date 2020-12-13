using System.Collections.Generic;

namespace HacknetSharp.Server
{
    public class ServiceProcess : Process
    {
        private readonly ServiceContext _context;
        private readonly IEnumerator<YieldToken?> _enumerator;
        private YieldToken? _currentToken;
        private bool _cleaned;

        public ServiceProcess(ServiceContext context, Service service) : base(context)
        {
            _context = context;
            _enumerator = service.Run(context);
        }

        public override bool Update(IWorld world)
        {
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
        }
    }
}

using System;
using System.Collections.Generic;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a running service.
    /// </summary>
    public class ServiceProcess : Process
    {
        private readonly ServiceContext _context;
        private readonly IEnumerator<YieldToken?> _enumerator;
        private readonly Func<ServiceContext, bool> _shutdownCallback;
        private YieldToken? _currentToken;
        private bool _cleaned;

        /// <summary>
        /// Creates a new instance of <see cref="ServiceProcess"/>.
        /// </summary>
        /// <param name="context">Service context.</param>
        /// <param name="service">Service this process will use.</param>
        public ServiceProcess(ServiceContext context, Service service) : base(context, service)
        {
            _context = context;
            _enumerator = service.Run(context);
            _shutdownCallback = service.OnShutdown;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override bool Complete(CompletionKind completionKind)
        {
            if (_cleaned) return true;
            _cleaned = true;
            if (_shutdownCallback(_context))
            {
                Completed = completionKind;
                return true;
            }

            return false;
        }
    }
}

using System.Collections.Generic;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a running service.
    /// </summary>
    public class ServiceProcess : Process
    {
        /// <summary>
        /// Context associated with this process.
        /// </summary>
        public ServiceContext ServiceContext { get; }

        private IEnumerator<YieldToken?>? _enumerator;
        private YieldToken? _currentToken;
        private bool _cleaned;

        /// <summary>
        /// Creates a new instance of <see cref="ServiceProcess"/>.
        /// </summary>
        /// <param name="service">Service this process will use.</param>
        public ServiceProcess(Service service) : base(service)
        {
            ServiceContext = service.Context;
        }

        /// <inheritdoc />
        public override bool Update(IWorld world)
        {
            _enumerator ??= Executable.Run();
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
            if (!Executable.OnShutdown()) return false;
            _cleaned = true;
            Completed = completionKind;
            return true;
        }
    }
}

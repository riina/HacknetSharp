using System.Threading;

namespace HacknetSharp.Server
{
    public class World
    {
        private readonly AutoResetEvent _waitHandle;

        internal World()
        {
            _waitHandle = new AutoResetEvent(true);
        }

        public void Tick()
        {
        }
    }
}

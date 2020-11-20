using System.Threading;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server
{
    public class WorldInstance : World
    {
        private readonly AutoResetEvent _waitHandle;

        internal WorldInstance()
        {
            _waitHandle = new AutoResetEvent(true);
        }

        public void Tick()
        {
        }
    }
}

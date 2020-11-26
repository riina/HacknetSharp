using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public abstract class System
    {
        public IWorld World { get; }
        public SystemModel? Model { get; protected set; }

        protected System(IWorld world, SystemModel? model = null)
        {
            World = world;
            Model = model;
        }
    }
}

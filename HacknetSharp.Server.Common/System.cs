using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public abstract class System
    {
        public World World { get; }
        public SystemModel? Model { get; protected set; }

        protected System(World world, SystemModel? model = null)
        {
            World = world;
            Model = model;
        }
    }
}

using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public class System
    {
        public IWorld World { get; }
        public SystemModel? Model { get; protected set; }

        public System(IWorld world, SystemModel? model = null)
        {
            World = world;
            Model = model;
        }
    }
}

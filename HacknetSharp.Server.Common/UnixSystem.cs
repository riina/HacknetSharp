using System.Linq;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public class UnixSystem : System
    {
        public UnixSystem(World world, SystemModel model) : base(world, model)
        {
        }

        public UnixSystem(World world, PersonModel owner, string name) : base(world)
        {
            Model = Spawn.System(this, owner, name);
            world.RegisterModel(Model);
            Model.Folders.AddRange(
                new[]
                {
                    "/bin", "/etc", "/home", "/lib", "/mnt", "/root", "/usr", "/usr/bin", "/usr/lib", "/usr/local",
                    "/usr/share", "/var", "/var/spool"
                }.Select(s =>
                    Spawn.Folder(this, s)));
        }
    }
}

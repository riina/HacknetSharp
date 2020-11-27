using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public interface ISpawn
    {
        PersonModel Person(IWorld context, string name, string userName, PlayerModel? player = null);

        SystemModel System(IWorld context, PersonModel owner, string name, SystemTemplate template);

        FileModel Folder(IWorld context, SystemModel owner, string name, string path);

        WorldModel World(string name, WorldTemplate template);
    }
}

using System.Collections.Generic;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public interface ISpawn
    {
        PersonModel Person(IWorld context, string name, string userName, PlayerModel? player = null);

        SystemModel System(IWorld context, PersonModel owner, string name, SystemTemplate template);

        WorldModel World(string name, WorldTemplate template);
    }
}

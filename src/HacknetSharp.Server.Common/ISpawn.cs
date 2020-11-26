using System.Collections.Generic;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public interface ISpawn
    {
        PersonModel Person(System context, string name, string userName);

        SystemModel? System(System context, PersonModel owner, string name, string template);

        (WorldModel, List<PersonModel>, List<SystemModel>)? World(string name, string template);
    }
}

using HacknetSharp.Server.Common.Models;
using HacknetSharp.Server.Common.Templates;

namespace HacknetSharp.Server.Common
{
    public interface ISpawn
    {
        PersonModel Person(WorldModel context, string name, string userName, PlayerModel? player = null);

        SystemModel System(WorldModel context, SystemTemplate template, PersonModel owner, string base64Hash, string base64Salt);

        FileModel Folder(WorldModel context, SystemModel owner, string name, string path);

        WorldModel World(string name, TemplateGroup templates, WorldTemplate template);
    }
}

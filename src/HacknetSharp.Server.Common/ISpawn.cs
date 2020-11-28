using HacknetSharp.Server.Common.Models;
using HacknetSharp.Server.Common.Templates;

namespace HacknetSharp.Server.Common
{
    public interface ISpawn
    {
        PersonModel Person(WorldModel context, string name, string userName, PlayerModel? player = null);

        SystemModel System(WorldModel context, SystemTemplate template, PersonModel owner, byte[] hash, byte[] salt);

        LoginModel Login(WorldModel context, SystemModel owner, string user, byte[] hash, byte[] salt,
            PersonModel? person = null);

        FileModel FileFile(WorldModel context, SystemModel owner, string name, string path, string file);

        FileModel Folder(WorldModel context, SystemModel owner, string name, string path);

        FileModel TextFile(WorldModel context, SystemModel owner, string name, string path, string content);

        FileModel ProgFile(WorldModel context, SystemModel owner, string name, string path, string progCode);

        public FileModel Duplicate(WorldModel context, SystemModel owner, string name, string path, FileModel existing);

        WorldModel World(string name, TemplateGroup templates, WorldTemplate template);
    }
}

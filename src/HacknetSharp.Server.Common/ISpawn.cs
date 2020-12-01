using HacknetSharp.Server.Common.Models;
using HacknetSharp.Server.Common.Templates;

namespace HacknetSharp.Server.Common
{
    public interface ISpawn
    {
        PersonModel Person(IServerDatabase database, WorldModel context, string name, string userName,
            PlayerModel? player = null);

        SystemModel System(IServerDatabase database, WorldModel context, SystemTemplate template, PersonModel owner,
            byte[] hash, byte[] salt,
            IPAddressRange range);

        SystemModel System(IServerDatabase database, WorldModel context, SystemTemplate template, PersonModel owner,
            byte[] hash, byte[] salt,
            uint address);

        LoginModel Login(IServerDatabase database, WorldModel context, SystemModel owner, string user, byte[] hash,
            byte[] salt,
            PersonModel? person = null);

        FileModel FileFile(IServerDatabase database, WorldModel context, SystemModel owner, string name, string path,
            string file);

        FileModel Folder(IServerDatabase database, WorldModel context, SystemModel owner, string name, string path);

        FileModel TextFile(IServerDatabase database, WorldModel context, SystemModel owner, string name, string path,
            string content);

        FileModel ProgFile(IServerDatabase database, WorldModel context, SystemModel owner, string name, string path,
            string progCode);

        public FileModel Duplicate(IServerDatabase database, WorldModel context, SystemModel owner, string name,
            string path, FileModel existing);

        WorldModel World(IServerDatabase database, string name, TemplateGroup templates, WorldTemplate template);
    }
}

using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;

namespace HacknetSharp.Server
{
    public interface ISpawn
    {
        PersonModel Person(IServerDatabase database, WorldModel context, string name, string userName,
            PlayerModel? player = null);

        SystemModel System(IServerDatabase database, WorldModel context, SystemTemplate template, PersonModel owner,
            byte[] hash, byte[] salt, IPAddressRange range);

        SystemModel System(IServerDatabase database, WorldModel context, SystemTemplate template, PersonModel owner,
            byte[] hash, byte[] salt, uint address);

        KnownSystemModel Connection(IServerDatabase database, SystemModel from, SystemModel to);

        VulnerabilityModel Vulnerability(IServerDatabase database, WorldModel context, SystemModel system);

        LoginModel Login(IServerDatabase database, WorldModel context, SystemModel owner, string user, byte[] hash,
            byte[] salt, bool admin, PersonModel? person = null);

        FileModel FileFile(IServerDatabase database, WorldModel context, SystemModel system, LoginModel owner,
            string name, string path, string file, bool hidden = false);

        FileModel Folder(IServerDatabase database, WorldModel context, SystemModel system, LoginModel owner,
            string name, string path, bool hidden = false);

        FileModel TextFile(IServerDatabase database, WorldModel context, SystemModel system, LoginModel owner,
            string name, string path, string content, bool hidden = false);

        FileModel ProgFile(IServerDatabase database, WorldModel context, SystemModel system, LoginModel owner,
            string name, string path, string progCode, bool hidden = false);

        public FileModel Duplicate(IServerDatabase database, WorldModel context, SystemModel system, LoginModel owner,
            string name, string path, FileModel existing, bool hidden = false);

        WorldModel World(IServerDatabase database, string name, TemplateGroup templates, WorldTemplate template);
    }
}

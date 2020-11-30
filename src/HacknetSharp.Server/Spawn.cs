using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HacknetSharp.Server.Common;
using HacknetSharp.Server.Common.Models;
using HacknetSharp.Server.Common.Templates;

namespace HacknetSharp.Server
{
    public class Spawn : ISpawn
    {
        public PlayerModel Player(UserModel context)
        {
            var player = new PlayerModel {User = context, Key = context.Key, Identities = new HashSet<PersonModel>()};
            context.Player = player;
            return player;
        }

        public PersonModel Person(WorldModel context, string name, string userName, PlayerModel? player = null)
        {
            var person = new PersonModel
            {
                Key = Guid.NewGuid(),
                World = context,
                Name = name,
                UserName = userName,
                Systems = new HashSet<SystemModel>(),
                WorkingDirectory = "/",
                Player = player
            };
            player?.Identities.Add(person);
            context.Persons.Add(person);
            return person;
        }

        private readonly Random _random = new Random();

        public unsafe SystemModel System(WorldModel context, SystemTemplate template, PersonModel owner, byte[] hash,
            byte[] salt, IPAddressRange range)
        {
            (uint host, uint subnetMask) = range.GetIPv4HostAndSubnetMask();
            var systems = context.Systems.Where(s => ((s.Address & subnetMask) ^ host) == 0).Select(s => s.Address)
                .ToHashSet();
            if (range.PrefixBits != 0 && systems.Count >= 1 << (32 - range.PrefixBits))
                throw new ApplicationException("Specified range has been saturated");

            uint invSubnetMask = ~subnetMask;
            uint gen;
            Span<byte> span = new Span<byte>((byte*)&gen, 4);
            do _random.NextBytes(span);
            while (systems.Contains(gen & invSubnetMask));
            return System(context, template, owner, hash, salt, (gen & invSubnetMask) ^ host);
        }

        public SystemModel System(WorldModel context, SystemTemplate template, PersonModel owner, byte[] hash,
            byte[] salt, uint address)
        {
            var system = new SystemModel
            {
                Address = address,
                Key = Guid.NewGuid(),
                World = context,
                Owner = owner,
                Files = new HashSet<FileModel>(),
                Logins = new HashSet<LoginModel>()
            };
            template.ApplyTemplate(this, context, system, owner, hash, salt);
            owner.Systems.Add(system);
            context.Systems.Add(system);
            return system;
        }

        public LoginModel Login(WorldModel context, SystemModel owner, string user, byte[] hash, byte[] salt,
            PersonModel? person = null)
        {
            var login = new LoginModel
            {
                Key = Guid.NewGuid(),
                World = context,
                System = owner,
                User = user,
                Hash = hash,
                Salt = salt,
                Person = person
            };
            owner.Logins.Add(login);
            return login;
        }

        public FileModel Folder(WorldModel context, SystemModel owner, string name, string path)
        {
            if (owner.Files.Any(f => f.Path == path && f.Name == name))
                throw new IOException($"The specified path already exists: {Program.Combine(path, name)}");
            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = FileModel.FileKind.Folder,
                Name = name,
                Path = path,
                System = owner,
                World = context
            };
            owner.Files.Add(model);

            // Generate dependent folders
            if (path == "/") return model;
            var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
            if (!owner.Files.Any(f => f.Path == nPath && f.Name == nName))
                Folder(context, owner, nName, nPath);
            return model;
        }

        public FileModel FileFile(WorldModel context, SystemModel owner, string name, string path, string file)
        {
            if (owner.Files.Any(f => f.Path == path && f.Name == name))
                throw new IOException($"The specified path already exists: {Program.Combine(path, name)}");
            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = FileModel.FileKind.FileFile,
                Name = name,
                Path = path,
                System = owner,
                World = context,
                Content = file
            };
            owner.Files.Add(model);

            // Generate dependent folders
            if (path == "/") return model;
            var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
            if (!owner.Files.Any(f => f.Path == nPath && f.Name == nName))
                Folder(context, owner, nName, nPath);
            return model;
        }

        public FileModel TextFile(WorldModel context, SystemModel owner, string name, string path, string content)
        {
            if (owner.Files.Any(f => f.Path == path && f.Name == name))
                throw new IOException($"The specified path already exists: {Program.Combine(path, name)}");
            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = FileModel.FileKind.TextFile,
                Name = name,
                Path = path,
                System = owner,
                World = context,
                Content = content
            };
            owner.Files.Add(model);

            // Generate dependent folders
            if (path == "/") return model;
            var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
            if (!owner.Files.Any(f => f.Path == nPath && f.Name == nName))
                Folder(context, owner, nName, nPath);
            return model;
        }

        public FileModel ProgFile(WorldModel context, SystemModel owner, string name, string path, string progCode)
        {
            if (owner.Files.Any(f => f.Path == path && f.Name == name))
                throw new IOException($"The specified path already exists: {Program.Combine(path, name)}");
            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = FileModel.FileKind.ProgFile,
                Name = name,
                Path = path,
                System = owner,
                World = context,
                Content = progCode
            };
            owner.Files.Add(model);

            // Generate dependent folders
            if (path == "/") return model;
            var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
            if (!owner.Files.Any(f => f.Path == nPath && f.Name == nName))
                Folder(context, owner, nName, nPath);
            return model;
        }

        public FileModel Duplicate(WorldModel context, SystemModel owner, string name, string path, FileModel existing)
        {
            if (owner.Files.Any(f => f.Path == path && f.Name == name))
                throw new IOException($"The specified path already exists: {Program.Combine(path, name)}");
            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = existing.Kind,
                Name = name,
                Path = path,
                System = owner,
                World = context,
                Content = existing.Content
            };
            owner.Files.Add(model);

            // Generate dependent folders
            if (path == "/") return model;
            var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
            if (!owner.Files.Any(f => f.Path == nPath && f.Name == nName))
                Folder(context, owner, nName, nPath);
            return model;
        }

        public WorldModel World(string name, TemplateGroup templates, WorldTemplate template)
        {
            var world = new WorldModel
            {
                Key = Guid.NewGuid(),
                Name = name,
                Persons = new HashSet<PersonModel>(),
                Systems = new HashSet<SystemModel>()
            };
            template.ApplyTemplate(this, templates, world);
            return world;
        }
    }
}

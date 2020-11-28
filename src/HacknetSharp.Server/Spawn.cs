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
            return new PlayerModel {Key = context.Key, Identities = new HashSet<PersonModel>()};
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

        public SystemModel System(WorldModel context, SystemTemplate template, PersonModel owner, string base64Hash,
            string base64Salt)
        {
            var system = new SystemModel
            {
                Key = Guid.NewGuid(), World = context, Owner = owner, Files = new HashSet<FileModel>()
            };
            template.ApplyTemplate(this, system, owner, base64Hash, base64Salt);
            owner.Systems.Add(system);
            context.Systems.Add(system);
            return system;
        }

        public LoginModel Login(WorldModel context, SystemModel owner, string user, string pass,
            PersonModel? person = null)
        {
            var login = new LoginModel
            {
                Key = Guid.NewGuid(),
                World = context,
                System = owner,
                User = user,
                Pass = pass,
                Person = person
            };
            owner.Logins.Add(login);
            return login;
        }

        public FileModel Folder(WorldModel context, SystemModel owner, string name, string path)
        {
            var (nPath, nName) = (path, name);
            if (owner.Files.Any(f => f.Path == nPath && f.Name == nName))
                throw new IOException("The specified file already exists.");
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
            if (nPath != "/") Folder(context, owner, Program.GetDirectoryName(nPath)!, Program.GetFileName(nPath));

            return model;
        }

        public FileModel TextFile(WorldModel context, SystemModel owner, string name, string path, string content)
        {
            var (nPath, nName) = (path, name);
            if (owner.Files.Any(f => f.Path == nPath && f.Name == nName))
                throw new IOException("The specified file already exists.");
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
            if (nPath != "/") Folder(context, owner, Program.GetDirectoryName(nPath)!, Program.GetFileName(nPath));

            return model;
        }

        public FileModel ProgFile(WorldModel context, SystemModel owner, string name, string path, string progCode)
        {
            var (nPath, nName) = (path, name);
            if (owner.Files.Any(f => f.Path == nPath && f.Name == nName))
                throw new IOException("The specified file already exists.");
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
            if (nPath != "/") Folder(context, owner, Program.GetDirectoryName(nPath)!, Program.GetFileName(nPath));

            return model;
        }

        public FileModel Duplicate(WorldModel context, SystemModel owner, string name, string path, FileModel existing)
        {
            var (nPath, nName) = (path, name);
            if (owner.Files.Any(f => f.Path == nPath && f.Name == nName))
                throw new IOException("The specified file already exists.");
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
            if (nPath != "/") Folder(context, owner, Program.GetDirectoryName(nPath)!, Program.GetFileName(nPath));

            return model;
        }

        public WorldModel World(string name, TemplateGroup templates, WorldTemplate template)
        {
            var world = new WorldModel {Key = Guid.NewGuid(), Name = name};
            template.ApplyTemplate(this, templates, world);
            return world;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HacknetSharp.Server.Common;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server
{
    public class Spawn : ISpawn
    {
        public PlayerModel Player(UserModel context)
        {
            return new PlayerModel {Key = context.Key, Identities = new HashSet<PersonModel>()};
        }

        public PersonModel Person(IWorld context, string name, string userName, PlayerModel? player = null)
        {
            var person = new PersonModel
            {
                Key = Guid.NewGuid(),
                World = context.Model,
                Name = name,
                UserName = userName,
                Systems = new HashSet<SystemModel>(),
                WorkingDirectory = "/",
                Player = player
            };
            player?.Identities.Add(person);
            context.Model.Persons.Add(person);
            return person;
        }

        public SystemModel System(IWorld context, PersonModel owner, string name, SystemTemplate template)
        {
            var system = new SystemModel
            {
                Key = Guid.NewGuid(),
                World = context.Model,
                Owner = owner,
                Name = name,
                Files = new HashSet<FileModel>()
            };
            template.ApplyTemplate(system);
            owner.Systems.Add(system);
            context.Model.Systems.Add(system);
            return system;
        }

        public FileModel Folder(IWorld context, SystemModel owner, string name, string path)
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
                Owner = owner,
                World = context.Model
            };
            owner.Files.Add(model);

            // Generate dependent folders
            if (nPath != "/") Folder(context, owner, Program.GetDirectoryName(nPath)!, Program.GetFileName(nPath));

            return model;
        }

        public FileModel TextFile(IWorld context, SystemModel owner, string name, string path, string content)
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
                Owner = owner,
                World = context.Model,
                Content = content
            };
            owner.Files.Add(model);

            // Generate dependent folders
            if (nPath != "/") Folder(context, owner, Program.GetDirectoryName(nPath)!, Program.GetFileName(nPath));

            return model;
        }

        public FileModel ProgFile(IWorld context, SystemModel owner, string name, string path, string progCode)
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
                Owner = owner,
                World = context.Model,
                Content = progCode
            };
            owner.Files.Add(model);

            // Generate dependent folders
            if (nPath != "/") Folder(context, owner, Program.GetDirectoryName(nPath)!, Program.GetFileName(nPath));

            return model;
        }

        public FileModel Duplicate(IWorld context, SystemModel owner, string name, string path, FileModel existing)
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
                Owner = owner,
                World = context.Model,
                Content = existing.Content
            };
            owner.Files.Add(model);

            // Generate dependent folders
            if (nPath != "/") Folder(context, owner, Program.GetDirectoryName(nPath)!, Program.GetFileName(nPath));

            return model;
        }

        public WorldModel World(string name, WorldTemplate template)
        {
            var world = new WorldModel {Key = Guid.NewGuid(), Name = name};
            template.ApplyTemplate(world);
            return world;
        }
    }
}

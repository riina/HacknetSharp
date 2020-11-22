using System;
using System.Collections.Generic;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public static class Spawn
    {
        public static PersonModel Person(System context, string name, string userName)
        {
            return new PersonModel
            {
                Key = Guid.NewGuid(),
                World = context.World.Id,
                Name = name,
                UserName = userName,
                Systems = new List<SystemModel>()
            };
        }

        public static SystemModel System(System context, PersonModel owner, string name, string template)
        {
            // TODO search for template and apply
            return new SystemModel
            {
                Key = Guid.NewGuid(),
                World = context.World.Id,
                Owner = owner,
                Name = name,
                Folders = new List<FolderModel>(),
                SimpleFiles = new List<SimpleFileModel>()
            };
        }

        public static FolderModel Folder(System context, string path)
        {
            return new FolderModel
            {
                Key = Guid.NewGuid(),
                World = context.World.Id,
                Owner = context.Model ??
                        throw new ArgumentException($"{nameof(context)}.{nameof(context.Model)} was null"),
                Path = path
            };
        }
    }
}

using System;
using System.Collections.Generic;
using HacknetSharp.Server.Common;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server
{
    public class Spawn : ISpawn
    {
        private readonly TemplateGroup _templates;

        public Spawn(TemplateGroup templates)
        {
            _templates = templates;
        }

        public PersonModel Person(Common.System context, string name, string userName)
        {
            return new PersonModel
            {
                Key = Guid.NewGuid(),
                World = context.World.Model,
                Name = name,
                UserName = userName,
                Systems = new List<SystemModel>()
            };
        }

        public SystemModel? System(Common.System context, PersonModel owner, string name, string template)
        {
            // TODO search for template and apply
            return new SystemModel
            {
                Key = Guid.NewGuid(),
                World = context.World.Model,
                Owner = owner,
                Name = name,
                Files = new List<FileModel>()
            };
        }

        public (WorldModel, List<PersonModel>, List<SystemModel>)? World(string name, string template)
        {
            // TODO search for template and apply
            return (new WorldModel {Key = Guid.NewGuid(), Name = name}, new List<PersonModel>(),
                new List<SystemModel>());
        }
    }
}

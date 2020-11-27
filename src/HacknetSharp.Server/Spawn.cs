using System;
using System.Collections.Generic;
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
                Systems = new HashSet<SystemModel>()
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

        public WorldModel World(string name, WorldTemplate template)
        {
            var world = new WorldModel {Key = Guid.NewGuid(), Name = name};
            template.ApplyTemplate(world);
            return world;
        }
    }
}

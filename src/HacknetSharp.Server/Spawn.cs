using System;
using System.Collections.Generic;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;

namespace HacknetSharp.Server
{
    public class Spawn
    {
        protected readonly IServerDatabase _database;

        public Spawn(IServerDatabase database)
        {
            _database = database;
        }

        public UserModel User(string name, byte[] hash, byte[] salt, bool admin)
        {
            var user = new UserModel
            {
                Key = name,
                Hash = hash,
                Salt = salt,
                Admin = admin,
                Identities = new HashSet<PersonModel>()
            };
            _database.Add(user);
            return user;
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
            template.ApplyTemplate(_database, templates, world);
            _database.Add(world);
            return world;
        }

        public void RemoveUser(UserModel user, bool isCascade = false)
        {
            foreach (var person in user.Identities)
            {
                var worldSpawn = new WorldSpawn(_database, person.World);
                foreach (var system in person.Systems) worldSpawn.RemoveSystem(system);
            }

            if (isCascade) return;
            _database.Delete(user);
        }

        public void RemoveWorld(WorldModel world, bool isCascade = false)
        {
            var worldSpawn = new WorldSpawn(_database, world);
            foreach (var person in world.Persons)
                worldSpawn.RemovePerson(person, true);
            if (isCascade) return;
            _database.Delete(world);
        }
    }
}

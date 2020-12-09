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
            template.ApplyTemplate(_database, this, templates, world);
            _database.Add(world);
            return world;
        }

        public void RemoveUser(UserModel user, bool isCascade = false)
        {
            foreach (var person in user.Identities)
            {
                foreach (var system in person.Systems) _database.DeleteBulk(system.Files);
                _database.DeleteBulk(person.Systems);
            }

            if (isCascade) return;
            _database.DeleteBulk(user.Identities);
            _database.Delete(user);
        }

        public void RemoveWorld(WorldModel world, bool isCascade = false)
        {
            var worldSpawn = new WorldSpawn(_database, world);
            foreach (var person in world.Persons)
                worldSpawn.RemovePerson(person, true);
            if (isCascade) return;
            _database.DeleteBulk(world.Persons);
            _database.DeleteBulk(world.Systems);
            _database.Delete(world);
        }
    }
}

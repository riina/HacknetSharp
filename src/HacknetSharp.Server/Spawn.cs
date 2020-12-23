using System;
using System.Collections.Generic;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Contains server-level spawn utilities.
    /// </summary>
    public class Spawn
    {
        /// <summary>
        /// Server database this instance is backed by.
        /// </summary>
        protected IServerDatabase Database { get; }

        /// <summary>
        /// Creates a new instance of <see cref="Spawn"/>.
        /// </summary>
        /// <param name="database">Server database to use as backing.</param>
        public Spawn(IServerDatabase database)
        {
            Database = database;
        }

        /// <summary>
        /// Creates a new user.
        /// </summary>
        /// <param name="name">Username.</param>
        /// <param name="hash">Password hash.</param>
        /// <param name="salt">Password salt.</param>
        /// <param name="admin">If true, register as admin.</param>
        /// <returns>Generated model.</returns>
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
            Database.Add(user);
            return user;
        }

        /// <summary>
        /// Creates a new world.
        /// </summary>
        /// <param name="name">World name.</param>
        /// <param name="templates">Templates to use.</param>
        /// <param name="template">Template to apply.</param>
        /// <returns>Generated model.</returns>
        public WorldModel World(string name, TemplateGroup templates, WorldTemplate template)
        {
            var world = new WorldModel
            {
                Key = Guid.NewGuid(),
                Name = name,
                Persons = new HashSet<PersonModel>(),
                Systems = new HashSet<SystemModel>()
            };
            template.ApplyTemplate(Database, templates, world);
            Database.Add(world);
            return world;
        }

        /// <summary>
        /// Removes a user.
        /// </summary>
        /// <param name="user">Model to remove.</param>
        /// <param name="isCascade">If true, does not directly delete from database.</param>
        public void RemoveUser(UserModel user, bool isCascade = false)
        {
            foreach (var person in user.Identities)
            {
                var worldSpawn = new WorldSpawn(Database, person.World);
                foreach (var mission in person.Missions) worldSpawn.RemoveMission(mission);
                foreach (var system in person.Systems) worldSpawn.RemoveSystem(system);
            }

            if (isCascade) return;
            Database.Delete(user);
        }

        /// <summary>
        /// Removes a world.
        /// </summary>
        /// <param name="world">Model to remove.</param>
        /// <param name="isCascade">If true, does not directly delete from database.</param>
        public void RemoveWorld(WorldModel world, bool isCascade = false)
        {
            var worldSpawn = new WorldSpawn(Database, world);
            foreach (var person in world.Persons)
                worldSpawn.RemovePerson(person, true);
            if (isCascade) return;
            Database.Delete(world);
        }
    }
}

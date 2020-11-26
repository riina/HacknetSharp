using System;
using System.Collections.Generic;
using HacknetSharp.Server.Common;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server
{
    public class World : IWorld
    {
        public WorldModel Model { get; }
        public ISpawn Spawn { get; }
        public IServerDatabase Database { get; }
        public List<object> RegistrationSet { get; }
        public List<object> DirtySet { get; }
        public List<object> DeregistrationSet { get; }

        internal World(WorldModel model, IServerDatabase database, TemplateGroup templates)
        {
            Model = model;
            Spawn = new Spawn(templates);
            Database = database;
            RegistrationSet = new List<object>();
            DirtySet = new List<object>();
            DeregistrationSet = new List<object>();
        }

        public void Tick()
        {
            // TODO update
        }

        public void RegisterModel<T>(Model<T> model) where T : IEquatable<T>
        {
            RegistrationSet.Add(model);
        }

        public void RegisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>
        {
            RegistrationSet.AddRange(models);
        }

        public void DirtyModel<T>(Model<T> model) where T : IEquatable<T>
        {
            DirtySet.Add(model);
        }

        public void DirtyModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>
        {
            DirtySet.AddRange(models);
        }

        public void DeregisterModel<T>(Model<T> model) where T : IEquatable<T>
        {
            DeregistrationSet.Add(model);
        }

        public void DeregisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>
        {
            DeregistrationSet.AddRange(models);
        }
    }
}

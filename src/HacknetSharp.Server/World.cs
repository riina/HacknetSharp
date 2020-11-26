using System;
using System.Collections.Generic;
using System.Threading;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server
{
    public class World : IWorld
    {
        public Guid Id { get; internal set; }

        private readonly AutoResetEvent _waitHandle;
        public List<object> RegistrationSet { get; }
        public List<object> DirtySet { get; }
        public List<object> DeregistrationSet { get; }

        internal World()
        {
            _waitHandle = new AutoResetEvent(true);
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

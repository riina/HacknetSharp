using System;
using System.Collections.Generic;

namespace HacknetSharp.Server.Common
{
    public abstract class World
    {
        public Guid Id { get; protected set; }
        public abstract void RegisterModel<T>(Model<T> model) where T : IEquatable<T>;
        public abstract void RegisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>;
        public abstract void DirtyModel<T>(Model<T> model) where T : IEquatable<T>;
        public abstract void DirtyModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>;
        public abstract void DeregisterModel<T>(Model<T> model) where T : IEquatable<T>;
        public abstract void DeregisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>;
    }
}

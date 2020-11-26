using System;
using System.Collections.Generic;

namespace HacknetSharp.Server.Common
{
    public interface IWorld
    {
        Guid Id { get; }
        void RegisterModel<T>(Model<T> model) where T : IEquatable<T>;
        void RegisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>;
        void DirtyModel<T>(Model<T> model) where T : IEquatable<T>;
        void DirtyModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>;
        void DeregisterModel<T>(Model<T> model) where T : IEquatable<T>;
        void DeregisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>;
    }
}

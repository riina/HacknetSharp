using System;
using System.Collections.Generic;
using HacknetSharp.Server.Common.Models;
using HacknetSharp.Server.Common.Templates;

namespace HacknetSharp.Server.Common
{
    public interface IWorld
    {
        WorldModel Model { get; }
        ISpawn Spawn { get; }
        IServerDatabase Database { get; }
        SystemTemplate PlayerSystemTemplate { get; }
        double Time { get; }
        double PreviousTime { get; }
        void ExecuteCommand(CommandContext commandContext);
        void RegisterModel<T>(Model<T> model) where T : IEquatable<T>;
        void RegisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>;
        void DirtyModel<T>(Model<T> model) where T : IEquatable<T>;
        void DirtyModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>;
        void DeregisterModel<T>(Model<T> model) where T : IEquatable<T>;
        void DeregisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>;
    }
}

using System;
using System.Collections.Generic;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;

namespace HacknetSharp.Server
{
    public interface IWorld
    {
        WorldModel Model { get; }
        WorldSpawn Spawn { get; }
        IServerDatabase Database { get; }
        SystemTemplate PlayerSystemTemplate { get; }
        double Time { get; }
        double PreviousTime { get; }
        void CompleteRecurse(Process process, Process.CompletionKind completionKind);

        void StartShell(IPersonContext personContext, PersonModel personModel, SystemModel systemModel,
            LoginModel loginModel, string line);

        ProgramProcess? StartProgram(IPersonContext personContext, PersonModel personModel, SystemModel systemModel,
            LoginModel loginModel, string line);

        ServiceProcess? StartService(PersonModel personModel, SystemModel systemModel, LoginModel loginModel,
            string line);

        void ExecuteCommand(ProgramContext programContext);
        void RegisterModel<T>(Model<T> model) where T : IEquatable<T>;
        void RegisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>;
        void DirtyModel<T>(Model<T> model) where T : IEquatable<T>;
        void DirtyModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>;
        void DeregisterModel<T>(Model<T> model) where T : IEquatable<T>;
        void DeregisterModels<T>(IEnumerable<Model<T>> models) where T : IEquatable<T>;
    }
}

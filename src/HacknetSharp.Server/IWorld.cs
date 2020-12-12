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
        IEnumerable<(Program, ProgramInfoAttribute)> IntrinsicPrograms { get; }
        double Time { get; }
        double PreviousTime { get; }
        void CompleteRecurse(Process process, Process.CompletionKind completionKind);

        void StartShell(IPersonContext personContext, PersonModel personModel, SystemModel systemModel,
            LoginModel loginModel, string line);

        ProgramProcess? StartProgram(IPersonContext personContext, PersonModel personModel, SystemModel systemModel,
            LoginModel loginModel, string line);

        ServiceProcess? StartService(PersonModel personModel, SystemModel systemModel, LoginModel loginModel,
            string line);

        ProgramInfoAttribute? GetProgramInfo(string? argv);
        void ExecuteCommand(ProgramContext programContext);
    }
}

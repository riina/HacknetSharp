using System.Collections.Generic;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a simulation world.
    /// </summary>
    public interface IWorld
    {
        /// <summary>
        /// Database model for the world.
        /// </summary>
        WorldModel Model { get; }

        /// <summary>
        /// Spawn/despawn manager.
        /// </summary>
        WorldSpawn Spawn { get; }

        /// <summary>
        /// Backing database reference.
        /// </summary>
        IServerDatabase Database { get; }

        /// <summary>
        /// Player system template.
        /// </summary>
        SystemTemplate PlayerSystemTemplate { get; }

        /// <summary>
        /// Intrinsic programs in this world.
        /// </summary>
        IEnumerable<(Program, ProgramInfoAttribute)> IntrinsicPrograms { get; }

        /// <summary>
        /// Current world time.
        /// </summary>
        double Time { get; }

        /// <summary>
        /// Previous update's world time.
        /// </summary>
        double PreviousTime { get; }

        /// <summary>
        /// Completes the specified process recursively with the specified completion kind.
        /// </summary>
        /// <param name="process">Process to complete.</param>
        /// <param name="completionKind">Completion kind.</param>
        void CompleteRecurse(Process process, Process.CompletionKind completionKind);

        /// <summary>
        /// Attempts to start a shell.
        /// </summary>
        /// <param name="personContext">User context.</param>
        /// <param name="personModel">Person.</param>
        /// <param name="systemModel">System.</param>
        /// <param name="loginModel">Login.</param>
        /// <param name="line">Command line.</param>
        /// <returns>Started shell or null on failure conditions.</returns>
        /// <remarks>
        /// Process creation can fail if there are no remaining PIDs.
        /// </remarks>
        ShellProcess? StartShell(IPersonContext personContext, PersonModel personModel, SystemModel systemModel,
            LoginModel loginModel, string line);

        /// <summary>
        /// Attempts to start a program.
        /// </summary>
        /// <param name="personContext">User context.</param>
        /// <param name="personModel">Person.</param>
        /// <param name="systemModel">System.</param>
        /// <param name="loginModel">Login.</param>
        /// <param name="line">Command line.</param>
        /// <returns>Started program or null on failure conditions.</returns>
        /// <remarks>
        /// Process creation can fail for several reasons, including if there are no remaining PIDs
        /// or the executable doesn't exist, or if there isn't an active shell.
        /// </remarks>
        ProgramProcess? StartProgram(IPersonContext personContext, PersonModel personModel, SystemModel systemModel,
            LoginModel loginModel, string line);


        /// <summary>
        /// Attempts to start a service.
        /// </summary>
        /// <param name="personModel">Person.</param>
        /// <param name="systemModel">System.</param>
        /// <param name="loginModel">Login.</param>
        /// <param name="line">Command line.</param>
        /// <returns>Started program or null on failure conditions.</returns>
        /// <remarks>
        /// Process creation can fail for a several reasons, including if there are no remaining PIDs
        /// or the executable doesn't exist.
        /// </remarks>
        ServiceProcess? StartService(PersonModel personModel, SystemModel systemModel, LoginModel loginModel,
            string line);

        /// <summary>
        /// Attempts to get a program's information from the specified command line.
        /// </summary>
        /// <param name="argv">Command line to get program information from.</param>
        /// <returns>Program information or null if program not found.</returns>
        ProgramInfoAttribute? GetProgramInfo(string? argv);

        /// <summary>
        /// Executes a command that was prepared with <see cref="ServerUtil.InitTentativeProgramContext"/>.
        /// </summary>
        /// <param name="programContext">Program context to execute.</param>
        void ExecuteCommand(ProgramContext programContext);
    }
}

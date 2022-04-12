using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HacknetSharp.Server.Lua;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;
using Microsoft.Extensions.Logging;
using MoonSharp.Interpreter;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a simulation world.
    /// </summary>
    public interface IWorld
    {
        /// <summary>
        /// Script manager for this world.
        /// </summary>
        ScriptManager ScriptManager { get; }

        /// <summary>
        /// Templates available to world.
        /// </summary>
        TemplateGroup Templates { get; }

        /// <summary>
        /// Log receiver.
        /// </summary>
        ILogger Logger { get; }

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
        IEnumerable<(Func<Program>, ProgramInfoAttribute)> IntrinsicPrograms { get; }

        /// <summary>
        /// Current world time.
        /// </summary>
        double Time { get; }

        /// <summary>
        /// Previous update's world time.
        /// </summary>
        double PreviousTime { get; }

        /// <summary>
        /// Attempts to find a system with the specified ID.
        /// </summary>
        /// <param name="id">System ID.</param>
        /// <param name="system">Retrieved system.</param>
        /// <returns>True if system found.</returns>
        bool TryGetSystem(Guid id, [NotNullWhen(true)] out SystemModel? system);

        /// <summary>
        /// Searches for systems with the specified parameters.
        /// </summary>
        /// <param name="key">Group key.</param>
        /// <param name="tag">Tag.</param>
        /// <returns>Retrieved systems.</returns>
        IEnumerable<SystemModel> SearchSystems(Guid? key, string? tag);

        /// <summary>
        /// Searches for persons with the specified parameters.
        /// </summary>
        /// <param name="key">Group key.</param>
        /// <param name="tag">Tag.</param>
        /// <returns>Retrieved systems.</returns>
        IEnumerable<PersonModel> SearchPersons(Guid? key, string? tag);

        /// <summary>
        /// Attempts to complete the specified process recursively with the specified completion kind.
        /// </summary>
        /// <param name="process">Process to complete.</param>
        /// <param name="completionKind">Completion kind.</param>
        /// <returns>False if the process failed to terminate.</returns>
        bool CompleteRecurse(Process process, Process.CompletionKind completionKind);

        /// <summary>
        /// Attempts to start a shell.
        /// </summary>
        /// <param name="personContext">User context.</param>
        /// <param name="personModel">Person.</param>
        /// <param name="loginModel">Login.</param>
        /// <param name="argv">Command line.</param>
        /// <param name="attach">If true, automatically attach to shell chain.</param>
        /// <returns>Started shell or null on failure conditions.</returns>
        /// <remarks>
        /// Process creation can fail if there are no remaining PIDs.
        /// </remarks>
        ShellProcess? StartShell(IPersonContext personContext, PersonModel personModel, LoginModel loginModel,
            string[] argv, bool attach);

        /// <summary>
        /// Attempts to start a program.
        /// </summary>
        /// <param name="shell">Shell.</param>
        /// <param name="argv">Command line.</param>
        /// <param name="hargv">Hidden arguments.</param>
        /// <param name="program">Existing program to execute directly.</param>
        /// <returns>Started program or null on failure conditions.</returns>
        /// <remarks>
        /// Process creation can fail for several reasons, including if there are no remaining PIDs
        /// or the executable doesn't exist, or if there isn't an active shell.
        /// </remarks>
        ProgramProcess? StartProgram(ShellProcess shell, string[] argv, string[]? hargv = null,
            Program? program = null);

        /// <summary>
        /// Attempts to start a service.
        /// </summary>
        /// <param name="loginModel">Login.</param>
        /// <param name="argv">Command line.</param>
        /// <param name="hargv">Hidden arguments.</param>
        /// <param name="service">Existing service to execute directly.</param>
        /// <returns>Started program or null on failure conditions.</returns>
        /// <remarks>
        /// Process creation can fail for a several reasons, including if there are no remaining PIDs
        /// or the executable doesn't exist.
        /// </remarks>
        ServiceProcess? StartService(LoginModel loginModel, string[] argv, string[]? hargv = null,
            Service? service = null);

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

        /// <summary>
        /// Starts a mission for the specified person.
        /// </summary>
        /// <param name="person">Target person.</param>
        /// <param name="missionPath">Mission template path.</param>
        /// <param name="campaignKey">Campaign key.</param>
        /// <returns>Started mission</returns>
        MissionModel? StartMission(PersonModel person, string missionPath, Guid campaignKey);

        /// <summary>
        /// Queues a mission for the specified person.
        /// </summary>
        /// <param name="person">Target person.</param>
        /// <param name="missionPath">Mission template path.</param>
        /// <param name="campaignKey">Campaign key.</param>
        void QueueMission(PersonModel person, string missionPath, Guid campaignKey);

        /// <summary>
        /// Tries to get a script file's function from the specified path.
        /// </summary>
        /// <param name="name">Path to search.</param>
        /// <param name="script">Function object.</param>
        /// <returns>True if successful.</returns>
        bool TryGetScriptFile(string name, [NotNullWhen(true)] out DynValue? script);

        /// <summary>
        /// Ticks world.
        /// </summary>
        void Tick();
    }
}

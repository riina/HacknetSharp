using HacknetSharp.Server.Models;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents an executable's operation context.
    /// </summary>
    public class ProcessContext
    {
        /// <summary>
        /// Parent process ID.
        /// </summary>
        public uint ParentPid { get; set; }

        /// <summary>
        /// Process ID.
        /// </summary>
        public uint Pid { get; set; }

        /// <summary>
        /// Memory used by this process.
        /// </summary>
        public long Memory { get; set; }

        /// <summary>
        /// World for the process.
        /// </summary>
        public IWorld World { get; set; } = null!;

        /// <summary>
        /// System for the process.
        /// </summary>
        public SystemModel System { get; set; } = null!;

        /// <summary>
        /// Login for the process.
        /// </summary>
        public LoginModel Login { get; set; } = null!;

        /// <summary>
        /// Arguments passed to the process.
        /// </summary>
        public string[] Argv { get; set; } = null!;

        /// <summary>
        /// Original argument string.
        /// </summary>
        public string Args { get; set; } = null!;

        /// <summary>
        /// Hidden arguments for this process.
        /// </summary>
        public string[] HArgv { get; set; } = null!;
    }
}

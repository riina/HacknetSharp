using HacknetSharp.Server.Models;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents an executable that runs in the background.
    /// </summary>
    public abstract class Service : Executable
    {
        /// <summary>
        /// Execution context.
        /// </summary>
        public ServiceContext Context { get; set; } = null!;

        /// <summary>
        /// Parent process ID.
        /// </summary>
        public uint ParentPid => Context.ParentPid;

        /// <summary>
        /// Process ID.
        /// </summary>
        public uint Pid => Context.Pid;

        /// <summary>
        /// Memory used by this process.
        /// </summary>
        public long Memory
        {
            get => Context.Memory;
            set => Context.Memory = value;
        }

        /// <summary>
        /// World for the process.
        /// </summary>
        public IWorld World => Context.World;

        /// <summary>
        /// System for the process.
        /// </summary>
        public SystemModel System => Context.System;

        /// <summary>
        /// Person for the process.
        /// </summary>
        public PersonModel Person => Context.Person;

        /// <summary>
        /// Login for the process.
        /// </summary>
        public LoginModel Login => Context.Login;

        /// <summary>
        /// Arguments passed to the process.
        /// </summary>
        public string[] Argv => Context.Argv;

        /// <summary>
        /// Hidden arguments for this process.
        /// </summary>
        public string[] HArgv => Context.HArgv;
    }
}

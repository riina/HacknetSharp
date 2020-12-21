using System;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a program-specific operation context.
    /// </summary>
    public class ProgramContext : ProcessContext
    {
        /// <summary>
        /// User/NPC context for the process.
        /// </summary>
        public IPersonContext User { get; set; } = null!;

        /// <summary>
        /// Shell process for the process.
        /// </summary>
        public ShellProcess Shell { get; set; } = null!;

        /// <summary>
        /// Operation ID for the process.
        /// </summary>
        public Guid OperationId { get; set; }

        /// <summary>
        /// Invocation type for the process.
        /// </summary>
        public InvocationType Type { get; set; }

        /// <summary>
        /// Console width the process was called with.
        /// </summary>
        public int ConWidth { get; set; } = -1;

        /// <summary>
        /// True if the context is for an AI character.
        /// </summary>
        public bool IsAi { get; set; }

        /// <summary>
        /// Optional post-execution command to execute.
        /// </summary>
        public string[]? ChainLine { get; set; }

        /// <summary>
        /// Remote shell this process is connected to.
        /// </summary>
        public ShellProcess? Remote { get; set; }

        /// <summary>
        /// Command invocation mode.
        /// </summary>
        public enum InvocationType
        {
            /// <summary>
            /// Normal command.
            /// </summary>
            Standard,

            /// <summary>
            /// Command called on system connect.
            /// </summary>
            Connect,

            /// <summary>
            /// Command called for first user registration.
            /// </summary>
            StartUp
        }
    }
}

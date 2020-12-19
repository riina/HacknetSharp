namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a running process on a system/
    /// </summary>
    public abstract class Process
    {
        /// <summary>
        /// Context for the process.
        /// </summary>
        public ProcessContext Context { get; }

        /// <summary>
        /// Method in which this process was completed if not null.
        /// </summary>
        public CompletionKind? Completed { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="Process"/>.
        /// </summary>
        /// <param name="context">Process context information.</param>
        protected Process(ProcessContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Update current operation.
        /// </summary>
        /// <returns>True when operation is complete.</returns>
        public abstract bool Update(IWorld world);

        /// <summary>
        /// Complete operation
        /// </summary>
        /// <param name="completionKind">Completion kind</param>
        /// <returns>False if process refuses to complete.</returns>
        public abstract bool Complete(CompletionKind completionKind);

        /// <summary>
        /// The way a process was completed.
        /// </summary>
        public enum CompletionKind
        {
            /// <summary>
            /// The process came to a natural stop or was closed with a natural shell close.
            /// </summary>
            Normal,

            /// <summary>
            /// The process was closed by a local kill command.
            /// </summary>
            KillLocal,

            /// <summary>
            /// The process crashed due to a system shutdown or other circumstances.
            /// </summary>
            KillRemote
        }
    }
}

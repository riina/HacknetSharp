namespace HacknetSharp.Server
{
    public abstract class Process
    {
        public ProcessContext Context { get; }
        public CompletionKind? Completed { get; set; }

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
        public abstract void Complete(CompletionKind completionKind);

        public enum CompletionKind
        {
            Normal,
            KillLocal,
            KillRemote

        }
    }
}

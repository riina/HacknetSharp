namespace HacknetSharp.Server
{
    public abstract class Process
    {
        public ProcessContext Context { get; }

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
        /// Kill operation
        /// </summary>
        public abstract void Kill();
    }
}

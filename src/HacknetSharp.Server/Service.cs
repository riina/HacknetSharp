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
    }
}

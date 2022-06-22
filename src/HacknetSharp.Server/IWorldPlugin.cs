namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a plugin.
    /// </summary>
    public interface IWorldPlugin
    {
        /// <summary>
        /// Initializes plugin with specified world.
        /// </summary>
        /// <param name="world">World.</param>
        void Initialize(IWorld world);

        /// <summary>
        /// Updates plugin on world tick.
        /// </summary>
        void Tick();

        /// <summary>
        /// Attempts to provide program instance.
        /// </summary>
        /// <param name="command">Original command.</param>
        /// <param name="line">Split line.</param>
        /// <param name="result">Result.</param>
        /// <returns>True if successful.</returns>
        bool TryProvideProgram(string command, string[] line, out (Program, ProgramInfoAttribute?, string[]) result);

        /// <summary>
        /// Attempts to provide service instance.
        /// </summary>
        /// <param name="command">Original command.</param>
        /// <param name="line">Split line.</param>
        /// <param name="result">Result.</param>
        /// <returns>True if successful.</returns>
        bool TryProvideService(string command, string[] line, out (Service, ServiceInfoAttribute?, string[]) result);
    }
}

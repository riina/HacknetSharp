namespace HacknetSharp
{
    /// <summary>
    /// Represents a connection that can either either active or not.
    /// </summary>
    public interface IConnection
    {
        /// <summary>
        /// Current connection state.
        /// </summary>
        public bool Connected { get; }
    }
}

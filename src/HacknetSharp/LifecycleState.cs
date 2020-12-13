namespace HacknetSharp
{
    /// <summary>
    /// Represents a state in a basic lifecycle.
    /// </summary>
    public enum LifecycleState
    {
        /// <summary>
        /// The instance has not yet been prepared.
        /// </summary>
        NotStarted,

        /// <summary>
        /// The instance is currently preparing to reach the <see cref="Active"/> state.
        /// </summary>
        Starting,

        /// <summary>
        /// The instance is currently performing its main operation.
        /// </summary>
        Active,

        /// <summary>
        /// The instance is not active and is currently preparing to reach the <see cref="Disposed"/> state.
        /// </summary>
        Dispose,

        /// <summary>
        /// The instance has finalized its dispensation.
        /// </summary>
        Disposed
    }
}

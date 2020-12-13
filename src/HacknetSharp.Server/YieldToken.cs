namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a yield token that can be used to temporarily stop execution of a program.
    /// </summary>
    public abstract class YieldToken
    {
        /// <summary>
        /// Checks yield condition of this token.
        /// </summary>
        /// <param name="world">World to check token against.</param>
        /// <returns>True if yield is over and execution should resume.</returns>
        public abstract bool Yield(IWorld world);
    }
}

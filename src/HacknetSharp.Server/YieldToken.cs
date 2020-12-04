namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a yield token that can be used to temporarily stop execution of a program.
    /// </summary>
    public abstract class YieldToken
    {
        public abstract bool Yield(IWorld world);
    }
}

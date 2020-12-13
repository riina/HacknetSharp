using System;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents a database entry that is specific to a world.
    /// </summary>
    /// <typeparam name="T">Key type.</typeparam>
    public abstract class WorldMember<T> : Model<T> where T : IEquatable<T>
    {
        /// <summary>
        /// World this model resides in.
        /// </summary>
        public virtual WorldModel World { get; set; } = null!;
    }
}

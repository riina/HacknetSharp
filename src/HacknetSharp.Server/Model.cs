using System;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a database model.
    /// </summary>
    /// <typeparam name="T">Key type.</typeparam>
    public abstract class Model<T> where T : IEquatable<T>
    {
        /// <summary>
        /// Key for the model in the database.
        /// </summary>
        public virtual T Key { get; set; } = default!;
    }
}

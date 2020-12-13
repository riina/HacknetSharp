using System;

namespace HacknetSharp
{
    /// <summary>
    /// Represents an operation with a unique identifier.
    /// </summary>
    public interface IOperation
    {
        /// <summary>
        /// Identifier for this operation object.
        /// </summary>
        public Guid Operation { get; }
    }
}

using System;

namespace HacknetSharp
{
    /// <summary>
    /// Represents an exception thrown upon receipt of nonconforming data.
    /// </summary>
    public class ProtocolException : Exception
    {
        /// <summary>
        /// Creates a new instance of <see cref="ProtocolException"/> with the specified message.
        /// </summary>
        /// <param name="message">Detail message.</param>
        public ProtocolException(string message) : base(message)
        {
        }
    }
}

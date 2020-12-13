using System.IO;

namespace HacknetSharp
{
    /// <summary>
    /// Represents a serializable message.
    /// </summary>
    public abstract class Event
    {
        /// <summary>
        /// Serializes this event.
        /// </summary>
        /// <param name="stream">Target stream.</param>
        public abstract void Serialize(Stream stream);

        /// <summary>
        /// Deserializes this event.
        /// </summary>
        /// <param name="stream">Source stream.</param>
        public abstract void Deserialize(Stream stream);
    }
}

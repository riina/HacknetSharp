using System.IO;

namespace HacknetSharp
{
    public abstract class Event
    {
        /// <summary>
        /// Serialize this event.
        /// </summary>
        /// <param name="stream">Target stream.</param>
        public abstract void Serialize(Stream stream);

        /// <summary>
        /// Deserialize this event.
        /// </summary>
        /// <param name="stream">Source stream.</param>
        public abstract void Deserialize(Stream stream);
    }
}

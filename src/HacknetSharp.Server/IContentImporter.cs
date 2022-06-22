using System.IO;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a utility to import arbitrarily typed content from a stream.
    /// </summary>
    public interface IContentImporter
    {
        /// <summary>
        /// Imports a file of the specified type.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <typeparam name="T">Type.</typeparam>
        /// <returns>Object or null.</returns>
        T? Import<T>(Stream stream) where T : class;
    }
}

using System.IO;
using HacknetSharp.Server;

namespace hss
{
    /// <summary>
    /// Represents a content importer for yaml data.
    /// </summary>
    public class YamlContentImporter : IContentImporter
    {
        /// <inheritdoc />
        public T Import<T>(Stream stream) => HssUtil.YamlDeserializer.Deserialize<T>(new StreamReader(stream));
    }
}

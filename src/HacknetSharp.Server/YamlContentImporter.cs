using System.IO;

namespace HacknetSharp.Server;

/// <summary>
/// Represents a content importer for yaml data.
/// </summary>
public class YamlContentImporter : IContentImporter
{
    /// <inheritdoc />
    public T Import<T>(Stream stream) => ServerUtil.YamlDeserializer.Deserialize<T>(new StreamReader(stream));
}

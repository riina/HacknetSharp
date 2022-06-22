using System.IO;

namespace HacknetSharp.Server.Lua
{
    /// <summary>
    /// Represents a content importer for lua definitions.
    /// </summary>
    public class LuaContentImporter : IContentImporter
    {
        /// <inheritdoc />
        public T? Import<T>(Stream stream) where T : class =>
            LuaModule.CreateScript().DoStream(stream).ToObject() switch
            {
                T t1 => t1,
                IProxyConversion<T> t2 => t2.Generate(),
                _ => null
            };
    }
}

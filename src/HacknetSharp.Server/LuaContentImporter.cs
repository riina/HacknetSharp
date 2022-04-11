using System.IO;
using MoonSharp.Interpreter;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a content importer for lua definitions.
    /// </summary>
    public class LuaContentImporter : IContentImporter
    {
        /// <inheritdoc />
        public T Import<T>(Stream stream)
        {
            Script script = ServerModule.CreateScript();
            return script.DoStream(stream).ToObject<T>();
        }
    }
}

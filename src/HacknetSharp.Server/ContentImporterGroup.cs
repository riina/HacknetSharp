using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a grouping of content importers by extension.
    /// </summary>
    public class ContentImporterGroup
    {
        /// <summary>
        /// Importers.
        /// </summary>
        public Dictionary<string, IContentImporter> Importers { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="ContentImporterGroup"/>.
        /// </summary>
        public ContentImporterGroup()
        {
            Importers = new Dictionary<string, IContentImporter>(StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ContentImporterGroup"/>.
        /// </summary>
        public ContentImporterGroup(params (string, IContentImporter)[] importers)
        {
            Importers = new Dictionary<string, IContentImporter>(importers.Select(v => new KeyValuePair<string, IContentImporter>(v.Item1, v.Item2)), StringComparer.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Attempts to import an object.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="path">File path.</param>
        /// <param name="result">Result (may be null even if considered successful).</param>
        /// <typeparam name="T">Type.</typeparam>
        /// <returns>True if successfully imported.</returns>
        public bool TryImport<T>(Stream stream, string path, out T? result)
        {
            if (Importers.TryGetValue(Path.GetExtension(path), out var importer))
            {
                result = importer.Import<T>(stream);
                return true;
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Attempts to import an object from disk.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <param name="result">Result (may be null even if considered successful).</param>
        /// <typeparam name="T">Type.</typeparam>
        /// <returns>True if successfully imported.</returns>
        public bool TryImport<T>(string path, out T? result)
        {
            using var stream = File.OpenRead(path);
            return TryImport(stream, path, out result);
        }

        /// <summary>
        /// Imports an object.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="path">File path.</param>
        /// <typeparam name="T">Type.</typeparam>
        /// <returns>Imported object or null.</returns>
        /// <exception cref="NotSupportedException">Thrown for unsupported extension.</exception>
        public T? Import<T>(Stream stream, string path)
        {
            return TryImport<T>(stream, path, out var result) ? result : throw new NotSupportedException();
        }

        /// <summary>
        /// Imports an object from disk.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <typeparam name="T">Type.</typeparam>
        /// <returns>Imported object or null.</returns>
        /// <exception cref="NotSupportedException">Thrown for unsupported extension.</exception>
        public T? Import<T>(string path)
        {
            using var stream = File.OpenRead(path);
            return Import<T>(stream, path);
        }

        /// <summary>
        /// Imports an object.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="path">File path.</param>
        /// <typeparam name="T">Type.</typeparam>
        /// <returns>Imported object.</returns>
        /// <exception cref="NotSupportedException">Thrown for unsupported extension.</exception>
        /// <exception cref="IOException">Thrown for null data.</exception>
        public T ImportNotNull<T>(Stream stream, string path)
        {
            return Import<T>(stream, path) ?? throw new IOException();
        }

        /// <summary>
        /// Imports an object from disk.
        /// </summary>
        /// <param name="path">File path.</param>
        /// <typeparam name="T">Type.</typeparam>
        /// <returns>Imported object.</returns>
        /// <exception cref="NotSupportedException">Thrown for unsupported extension.</exception>
        /// <exception cref="IOException">Thrown for null data.</exception>
        public T ImportNotNull<T>(string path)
        {
            using var stream = File.OpenRead(path);
            return ImportNotNull<T>(stream, path);
        }
    }
}

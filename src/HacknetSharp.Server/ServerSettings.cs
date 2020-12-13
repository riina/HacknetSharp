using System.Collections.Generic;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents server settings (meant to be serialized to disk and loaded on bootstrap).
    /// </summary>
    public class ServerSettings
    {
        /// <summary>
        /// External host to bind to.
        /// </summary>
        public string? Host { get; set; }

        /// <summary>
        /// External port to bind to.
        /// </summary>
        public ushort Port { get; set; } = Constants.DefaultPort;

        /// <summary>
        /// Database properties.
        /// </summary>
        public Dictionary<string, string>? DatabaseProperties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Default world players are added to.
        /// </summary>
        public string? DefaultWorld { get; set; }

        /// <summary>
        /// If true, enable logging features (primarily database query logging).
        /// </summary>
        public bool? EnableLogging { get; set; }
    }
}

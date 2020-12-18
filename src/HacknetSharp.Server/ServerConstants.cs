namespace HacknetSharp.Server
{
    /// <summary>
    /// Server-specific critical constants.
    /// </summary>
    public class ServerConstants
    {
        /// <summary>
        /// Path to extensions folder.
        /// </summary>
        public const string ExtensionsFolder = "extensions";

        /// <summary>
        /// Name of command shell. A reference to WILL (Will SHell).
        /// </summary>
        public const string ShellName = "wsh";

        /// <summary>
        /// Maximum file length.
        /// </summary>
        public const int MaxFileLength = 2000;

        /// <summary>
        /// Default disk capacity.
        /// </summary>
        public const int DefaultDiskCapacity = 10_000;

        /// <summary>
        /// Default system memory (bytes).
        /// </summary>
        public const long DefaultSystemMemory = 4_000_000_000;

        /// <summary>
        /// Log recording shell login.
        /// </summary>
        public const string LogKind_Login = "login";
    }
}

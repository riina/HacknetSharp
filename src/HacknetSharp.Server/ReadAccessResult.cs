namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents the result of an attempt to read a file.
    /// </summary>
    public enum ReadAccessResult
    {
        /// <summary>
        /// The file is readable.
        /// </summary>
        Readable,

        /// <summary>
        /// Either the file is not readable, or one of its parents is not readable.
        /// </summary>
        NotReadable,

        /// <summary>
        /// The parent location is readable but the file does not exist.
        /// </summary>
        NoExist
    }
}

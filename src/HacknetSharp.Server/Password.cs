namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a serializable password.
    /// </summary>
    public readonly struct Password
    {
        /// <summary>Password salt.</summary>
        public readonly byte[] Salt;

        /// <summary>Password hash.</summary>
        public readonly byte[] Hash;

        /// <summary>
        /// Represents a serializable password.
        /// </summary>
        /// <param name="salt">Password salt.</param>
        /// <param name="hash">Password hash.</param>
        public Password(byte[] salt, byte[] hash)
        {
            Salt = salt;
            Hash = hash;
        }
    }
}

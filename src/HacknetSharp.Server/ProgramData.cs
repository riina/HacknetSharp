using System;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Program data.
    /// </summary>
    public readonly struct ProgramData
    {
        /// <summary>
        /// Factory function.
        /// </summary>
        public readonly Func<Program> Func;
        /// <summary>
        /// Associated program info.
        /// </summary>
        public readonly ProgramInfoAttribute Info;

        /// <summary>
        /// Initializes a new instance of <see cref="ProgramData"/>.
        /// </summary>
        /// <param name="func">Factory function.</param>
        /// <param name="info">Associated program info.</param>
        public ProgramData(Func<Program> func, ProgramInfoAttribute info)
        {
            Func = func;
            Info = info;
        }
    }
}

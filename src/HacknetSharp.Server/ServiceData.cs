using System;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Service data.
    /// </summary>
    public readonly struct ServiceData
    {
        /// <summary>
        /// Factory function.
        /// </summary>
        public readonly Func<Service> Func;
        /// <summary>
        /// Associated service info.
        /// </summary>
        public readonly ServiceInfoAttribute Info;

        /// <summary>
        /// Initializes a new instance of <see cref="ServiceData"/>.
        /// </summary>
        /// <param name="func">Factory function.</param>
        /// <param name="info">Associated service info.</param>
        public ServiceData(Func<Service> func, ServiceInfoAttribute info)
        {
            Func = func;
            Info = info;
        }
    }
}

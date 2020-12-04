using System;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Provides information about a service.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceInfoAttribute : Attribute
    {
        /// <summary>
        /// Service codename, should be something unique like "core:sshd" or "Ryazan:REconstruction".
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Provides information about a program.
        /// </summary>
        /// <param name="name">Program name.</param>
        public ServiceInfoAttribute(string name)
        {
            Name = name;
        }
    }
}

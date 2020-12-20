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
        /// Program codename, should be something unique like "core:sshd" or "Ryazan:REconstruction".
        /// </summary>
        public string ProgCode { get; set; }

        /// <summary>
        /// Program name, like "sshd" or "ephyd".
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Default memory used at process start.
        /// </summary>
        public long Memory { get; set; }

        /// <summary>
        /// Provides information about a service.
        /// </summary>
        /// <param name="progCode">Service code.</param>
        /// <param name="name">Service name.</param>
        public ServiceInfoAttribute(string progCode, string name)
        {
            ProgCode = progCode;
            Name = name;
        }
    }
}

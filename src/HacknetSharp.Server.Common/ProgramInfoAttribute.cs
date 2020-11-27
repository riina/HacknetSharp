using System;

namespace HacknetSharp.Server.Common
{
    /// <summary>
    /// Provides information about a program.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ProgramInfoAttribute : Attribute
    {
        /// <summary>
        /// Program codename, should be something unique like "core:ls" or "Ryazan:REconstruction".
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Program description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Program usage definition text.
        /// </summary>
        public string Usage { get; set; }

        /// <summary>
        /// Provides information about a program.
        /// </summary>
        /// <param name="name">Program name.</param>
        /// <param name="description">Program description.</param>
        /// <param name="usage">Program usage definition text.</param>
        public ProgramInfoAttribute(string name, string description, string usage)
        {
            Name = name;
            Description = description;
            Usage = usage;
        }
    }
}

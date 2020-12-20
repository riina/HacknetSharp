using System;

namespace HacknetSharp.Server
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
        public string ProgCode { get; set; }

        /// <summary>
        /// Program name, like "ls" or "wish".
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Default memory used at process start.
        /// </summary>
        public long Memory { get; set; }

        /// <summary>
        /// Program description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Program long description.
        /// </summary>
        public string LongDescription { get; set; }

        /// <summary>
        /// Program usage format text.
        /// </summary>
        public string Usage { get; set; }

        /// <summary>
        /// If true, intrinsic command available on all systems.
        /// </summary>
        public bool Intrinsic { get; set; }

        /// <summary>
        /// Provides information about a program.
        /// </summary>
        /// <param name="progCode">Program code.</param>
        /// <param name="name">Program name.</param>
        /// <param name="description">Program description.</param>
        /// <param name="longDescription">Program long description.</param>
        /// <param name="usage">Program usage format text.</param>
        /// <param name="intrinsic">If true, intrinsic command available on all systems.</param>
        public ProgramInfoAttribute(string progCode, string name, string description, string longDescription,
            string usage, bool intrinsic)
        {
            ProgCode = progCode;
            Name = name;
            Description = description;
            LongDescription = longDescription;
            Usage = usage;
            Intrinsic = intrinsic;
        }
    }
}

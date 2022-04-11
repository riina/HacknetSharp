using System;
using MoonSharp.Interpreter;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents a time-based task.
    /// </summary>
    public class CronModel : WorldMember<Guid>
    {
        /// <summary>
        /// System this task belongs to.
        /// </summary>
        public virtual SystemModel System { get; set; } = null!;

        /// <summary>
        /// Script content.
        /// </summary>
        public virtual string Content { get; set; } = null!;

        /// <summary>
        /// Last time this task was run.
        /// </summary>
        public virtual double LastRunAt { get; set; }

        /// <summary>
        /// Task delay.
        /// </summary>
        public virtual double Delay { get; set; }

        /// <summary>
        /// Task end time.
        /// </summary>
        public virtual double End { get; set; }

        /// <summary>
        /// Task object.
        /// </summary>
        public DynValue? Task { get; set; } = null!;
    }
}

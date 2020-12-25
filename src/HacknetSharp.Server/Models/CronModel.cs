using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
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

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) => builder.Entity<CronModel>(x =>
        {
            x.HasKey(v => v.Key);
            x.Ignore(v => v.Task);
        });
#pragma warning restore 1591
    }
}

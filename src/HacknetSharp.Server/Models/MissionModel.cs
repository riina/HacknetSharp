using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents an active mission.
    /// </summary>
    public class MissionModel : WorldMember<Guid>
    {
        /// <summary>
        /// Campaign this mission belongs to.
        /// </summary>
        public virtual Guid CampaignKey { get; set; }

        /// <summary>
        /// Objective completion flags.
        /// </summary>
        public virtual long Flags { get; set; }

        /// <summary>
        /// Source template.
        /// </summary>
        public virtual string Template { get; set; } = null!;

        /// <summary>
        /// Person undertaking this mission.
        /// </summary>
        public virtual PersonModel Person { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<MissionModel>(x => x.HasKey(v => v.Key));
#pragma warning restore 1591
    }
}

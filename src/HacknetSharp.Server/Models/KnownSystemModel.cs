using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents a directed knowledge connection from one system to another.
    /// </summary>
    public class KnownSystemModel : WorldMember<Guid>
    {
        /// <summary>
        /// Property required for EF database.
        /// </summary>
        public Guid FromKey { get; set; }

        /// <summary>
        /// Property required for EF database.
        /// </summary>
        public Guid ToKey { get; set; }

        /// <summary>
        /// System that knows the other system.
        /// </summary>
        public virtual SystemModel From { get; set; } = null!;

        /// <summary>
        /// System that is known by the other system.
        /// </summary>
        public virtual SystemModel To { get; set; } = null!;

        /// <summary>
        /// If true, represents a local connection.
        /// </summary>
        public bool Local { get; set; }

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<KnownSystemModel>(x =>
            {
                x.HasKey(v => new {v.FromKey, v.ToKey});
                x.HasOne(e => e.From)
                    .WithMany(e => e.KnownSystems)
                    .HasForeignKey(e => e.FromKey);
                x.HasOne(e => e.To)
                    .WithMany(e => e.KnowingSystems)
                    .HasForeignKey(e => e.ToKey);
            });
#pragma warning restore 1591
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    public class KnownSystemModel : WorldMember<Guid>
    {
        public Guid FromKey { get; set; }
        public Guid ToKey { get; set; }

        public virtual SystemModel From { get; set; } = null!;
        public virtual SystemModel To { get; set; } = null!;
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

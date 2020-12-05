using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    public class WorldModel : Model<Guid>
    {
        public virtual string Name { get; set; } = null!;
        public virtual string Label { get; set; } = null!;
        public virtual string PlayerSystemTemplate { get; set; } = null!;
        public virtual string StartupCommandLine { get; set; } = null!;
        public virtual string PlayerAddressRange { get; set; } = null!;
        public virtual HashSet<PersonModel> Persons { get; set; } = null!;
        public virtual HashSet<SystemModel> Systems { get; set; } = null!;
        public virtual double Now { get; set; }

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<WorldModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(y => y.Persons).WithOne(z => z.World!).OnDelete(DeleteBehavior.Cascade);
                x.HasMany(y => y.Systems).WithOne(z => z.World!).OnDelete(DeleteBehavior.Cascade);
            });
#pragma warning restore 1591
    }
}

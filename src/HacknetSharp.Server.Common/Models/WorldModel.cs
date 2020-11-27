using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Common.Models
{
    public class WorldModel : Model<Guid>
    {
        public virtual string Name { get; set; } = null!;
        public virtual string SystemTemplate { get; set; } = null!;
        public virtual string StartupProgram { get; set; } = null!;
        public virtual string StartupCommandLine { get; set; } = null!;
        public virtual HashSet<PersonModel> Persons { get; set; } = null!;
        public virtual HashSet<SystemModel> Systems { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<WorldModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(x => x.Persons).WithOne(x => x.World!).OnDelete(DeleteBehavior.Cascade);
                x.HasMany(x => x.Systems).WithOne(x => x.World!).OnDelete(DeleteBehavior.Cascade);
            });
#pragma warning restore 1591
    }
}

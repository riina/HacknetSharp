using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Common.Models
{
    public class PersonModel : WorldMember<Guid>
    {
        public virtual string Name { get; set; } = null!;
        public virtual string UserName { get; set; } = null!;
        public virtual PlayerModel? Player { get; set; }
        public virtual SystemModel DefaultSystem { get; set; } = null!;
        public virtual SystemModel CurrentSystem { get; set; } = null!;
        public virtual HashSet<SystemModel> Systems { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<PersonModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(x => x.Systems).WithOne(x => x.Owner).OnDelete(DeleteBehavior.Cascade);
            });
#pragma warning restore 1591
    }
}

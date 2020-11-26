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
        public virtual List<SystemModel> Systems { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<PersonModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasOne(p => p.Player).WithMany(p => p!.Identities);
            });
#pragma warning restore 1591
    }
}

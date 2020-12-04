using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    public class PlayerModel : Model<string>
    {
        public virtual UserModel User { get; set; } = null!;
        public string UserForeignKey { get; set; } = null!;
        public virtual Guid ActiveWorld { get; set; }
        public virtual HashSet<PersonModel> Identities { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<PlayerModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(p => p!.Identities).WithOne(p => p.Player!).OnDelete(DeleteBehavior.Cascade);
            });
#pragma warning restore 1591
    }
}

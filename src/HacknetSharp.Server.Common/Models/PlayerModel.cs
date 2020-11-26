using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Common.Models
{
    public class PlayerModel : Model<string>
    {
        public readonly UserModel User = new UserModel();
        public virtual Guid ActiveWorld { get; set; }
        public virtual Guid DefaultSystem { get; set; }
        public virtual List<PersonModel> Identities { get; set; } = null!;
        public virtual SystemModel CurrentSystem { get; set; } = null!;

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

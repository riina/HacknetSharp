using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Common.Models
{
    public class PlayerModel : Model<Guid>
    {
        public Guid ActiveWorld { get; set; }
        public Guid DefaultSystem { get; set; }
        public SystemModel CurrentSystem { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<PlayerModel>(x => x.HasKey(v => v.Key));
#pragma warning restore 1591
    }
}

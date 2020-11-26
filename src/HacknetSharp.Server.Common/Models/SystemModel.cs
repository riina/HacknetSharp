using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Common.Models
{
    public class SystemModel : WorldMember<Guid>
    {
        public virtual string Name { get; set; } = null!;
        public virtual PersonModel Owner { get; set; } = null!;
        public virtual List<FileModel> Files { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<SystemModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(x => x.Files).WithOne(x => x.Owner).OnDelete(DeleteBehavior.Cascade);
            });
#pragma warning restore 1591
    }
}

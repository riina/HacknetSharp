using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Common.Models
{
    public class SystemModel : WorldMember<Guid>
    {
        public virtual string Name { get; set; } = null!;
        public virtual string OsName { get; set; } = null!;
        public virtual uint Address { get; set; }
        public virtual string? ConnectCommandLine { get; set; }
        public virtual PersonModel Owner { get; set; } = null!;
        public virtual HashSet<LoginModel> Logins { get; set; } = null!;
        public virtual HashSet<FileModel> Files { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<SystemModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(y => y.Files).WithOne(z => z.System).OnDelete(DeleteBehavior.Cascade);
                x.HasMany(y => y.Logins).WithOne(z => z.System).OnDelete(DeleteBehavior.Cascade);
            });
#pragma warning restore 1591
    }
}

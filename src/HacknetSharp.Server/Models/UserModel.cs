using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    public class UserModel : Model<string>
    {
        public virtual byte[] Hash { get; set; } = null!;
        public virtual byte[] Salt { get; set; } = null!;
        public virtual bool Admin { get; set; }
        public virtual Guid ActiveWorld { get; set; }
        public virtual HashSet<PersonModel> Identities { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<UserModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(p => p!.Identities).WithOne(p => p.User!).OnDelete(DeleteBehavior.Cascade);
            });
#pragma warning restore 1591
    }
}

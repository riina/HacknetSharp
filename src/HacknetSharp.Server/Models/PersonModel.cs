using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class PersonModel : WorldMember<Guid>
    {
        public virtual string Name { get; set; } = null!;
        public virtual string UserName { get; set; } = null!;
        public virtual PlayerModel? Player { get; set; }
        public virtual Guid DefaultSystem { get; set; }
        public List<ShellProcess> ShellChain { get; set; } = new List<ShellProcess>();
        public virtual bool StartedUp { get; set; }
        public virtual HashSet<SystemModel> Systems { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<PersonModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(y => y.Systems).WithOne(z => z.Owner).OnDelete(DeleteBehavior.Cascade);
                x.Ignore(v => v.ShellChain);
            });
#pragma warning restore 1591
    }
}

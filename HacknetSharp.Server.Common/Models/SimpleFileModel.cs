using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Common.Models
{
    public class SimpleFileModel : WorldMember<Guid>
    {
        public SystemModel Owner { get; set; } = null!;
        public string Path { get; set; } = null!;
        public string Content { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder)
        {
            builder.Entity<SimpleFileModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasOne(x => x.Owner).WithMany(x => x.SimpleFiles);
            });
        }
#pragma warning restore 1591
    }
}

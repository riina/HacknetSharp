using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.EF.Models
{
    /// <summary>
    /// Represents a directed knowledge connection from one system to another.
    /// </summary>
    public class KnownSystemModel : EFModelHelper
    {
        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<HacknetSharp.Server.Models.KnownSystemModel>(x =>
            {
                x.HasKey(v => new { v.FromKey, v.ToKey });
                x.HasOne(e => e.From)
                    .WithMany(e => e.KnownSystems)
                    .HasForeignKey(e => e.FromKey);
                x.HasOne(e => e.To)
                    .WithMany(e => e.KnowingSystems)
                    .HasForeignKey(e => e.ToKey);
            });
#pragma warning restore 1591
    }
}

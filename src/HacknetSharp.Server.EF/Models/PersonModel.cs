using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.EF.Models
{
    /// <summary>
    /// Represents a person who owns systems.
    /// </summary>
    public class PersonModel : EFModelHelper
    {
        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<HacknetSharp.Server.Models.PersonModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(y => y.Systems).WithOne(z => z.Owner).OnDelete(DeleteBehavior.Cascade);
                x.HasMany(y => y.Missions).WithOne(z => z.Person).OnDelete(DeleteBehavior.Cascade);
                x.Ignore(v => v.ShellChain);
            });
#pragma warning restore 1591
    }
}

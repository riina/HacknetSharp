using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.EF.Models
{
    /// <summary>
    /// Represents an instance of a world in a server.
    /// </summary>
    public class WorldModel : EFModelHelper
    {
        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<HacknetSharp.Server.Models.WorldModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(y => y.Persons).WithOne(z => z.World).OnDelete(DeleteBehavior.Cascade);
                x.HasMany(y => y.Systems).WithOne(z => z.World).OnDelete(DeleteBehavior.Cascade);
                x.Ignore(y => y.AddressedSystems);
                x.Ignore(y => y.TaggedSystems);
                x.Ignore(y => y.TaggedPersons);
                x.Ignore(y => y.ActiveMissions);
                x.Ignore(y => y.SpawnGroupPersons);
                x.Ignore(y => y.SpawnGroupSystems);
            });
#pragma warning restore 1591
    }
}

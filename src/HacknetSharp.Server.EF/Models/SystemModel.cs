using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.EF.Models
{
    /// <summary>
    /// Represents a networked device in a world.
    /// </summary>
    public class SystemModel : EFModelHelper
    {
        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<HacknetSharp.Server.Models.SystemModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(y => y.Files).WithOne(z => z.System).OnDelete(DeleteBehavior.Cascade);
                x.HasMany(y => y.Tasks).WithOne(z => z.System).OnDelete(DeleteBehavior.Cascade);
                x.HasMany(y => y.Logins).WithOne(z => z.System).OnDelete(DeleteBehavior.Cascade);
                x.HasMany(y => y.Vulnerabilities).WithOne(z => z.System).OnDelete(DeleteBehavior.Cascade);
                x.Ignore(y => y.Processes);
                x.Ignore(y => y.Pulse);
                //x.Ignore(y => y.PublicServices);
                x.Ignore(y => y.TargetingShells);
                //x.Ignore(y => y.VulnerabilityVersions);
                //x.Ignore(y => y.FirewallVersion);
                //x.Ignore(y => y.ProxyVersion);
            });
#pragma warning restore 1591
    }
}

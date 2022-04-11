using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.EF.Models
{
    /// <summary>
    /// Represents an active mission.
    /// </summary>
    public class MissionModel : EFModelHelper
    {
        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<HacknetSharp.Server.Models.MissionModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.Ignore(v => v.Data);
            });
#pragma warning restore 1591
    }
}

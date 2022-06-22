using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.EF.Models
{
    /// <summary>
    /// Represents a time-based task.
    /// </summary>
    public class CronModel : EFModelHelper
    {
        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) => builder.Entity<HacknetSharp.Server.Models.CronModel>(x =>
        {
            x.HasKey(v => v.Key);
        });
#pragma warning restore 1591
    }
}

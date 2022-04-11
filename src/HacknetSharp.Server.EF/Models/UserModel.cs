using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.EF.Models
{
    /// <summary>
    /// Represents a player's account.
    /// </summary>
    public class UserModel : EFModelHelper
    {
        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<HacknetSharp.Server.Models.UserModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(p => p.Identities).WithOne(p => p.User!).OnDelete(DeleteBehavior.Cascade);
                x.Ignore(v => v.Outputs);
            });
#pragma warning restore 1591
    }
}

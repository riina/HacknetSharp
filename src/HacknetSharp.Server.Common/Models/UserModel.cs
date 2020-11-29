using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Common.Models
{
    public class UserModel : Model<string>
    {
        public virtual byte[] Hash { get; set; } = null!;
        public virtual byte[] Salt { get; set; } = null!;
        public virtual bool Admin { get; set; }
        public virtual PlayerModel Player { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<UserModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasOne(y => y.Player).WithOne(z => z.User).HasForeignKey<PlayerModel>(p => p.UserForeignKey)
                    .OnDelete(DeleteBehavior.Cascade);
            });
#pragma warning restore 1591
    }
}

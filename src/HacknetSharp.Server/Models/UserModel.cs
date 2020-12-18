using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents a player's account.
    /// </summary>
    public class UserModel : Model<string>
    {
        /// <summary>
        /// Password hash.
        /// </summary>
        public virtual byte[] Hash { get; set; } = null!;

        /// <summary>
        /// Password salt.
        /// </summary>
        public virtual byte[] Salt { get; set; } = null!;

        /// <summary>
        /// If true, this user has admin privileges on the server.
        /// </summary>
        public virtual bool Admin { get; set; }

        /// <summary>
        /// Active password reset token.
        /// </summary>
        public virtual string? PasswordResetToken { get; set; }

        /// <summary>
        /// Expiry time of token in milliseconds since unix epoch.
        /// </summary>
        public virtual long PasswordResetTokenExpiry { get; set; }

        /// <summary>
        /// World this user is currently active in.
        /// </summary>
        public virtual Guid ActiveWorld { get; set; }

        /// <summary>
        /// Identities of this user across worlds the user is involved in.
        /// </summary>
        public virtual HashSet<PersonModel> Identities { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<UserModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(p => p!.Identities).WithOne(p => p.User!).OnDelete(DeleteBehavior.Cascade);
            });
#pragma warning restore 1591
    }
}

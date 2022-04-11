using System;
using System.Collections.Generic;

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

        /// <summary>
        /// Currently available outputs for server events.
        /// </summary>
        public HashSet<IPersonContext> Outputs { get; set; } = new();
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents a login for a system.
    /// </summary>
    public class LoginModel : WorldMember<Guid>
    {
        /// <summary>
        /// System this login works on.
        /// </summary>
        public virtual SystemModel System { get; set; } = null!;

        /// <summary>
        /// If true, user has admin privileges.
        /// </summary>
        public virtual bool Admin { get; set; }

        /// <summary>
        /// Associated person for this login, may be <see cref="Guid.Empty"/>.
        /// </summary>
        public virtual Guid Person { get; set; }

        /// <summary>
        /// Username of this login.
        /// </summary>
        public virtual string User { get; set; } = null!;

        /// <summary>
        /// Password hash.
        /// </summary>
        public virtual byte[] Hash { get; set; } = null!;

        /// <summary>
        /// Password salt.
        /// </summary>
        public virtual byte[] Salt { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<LoginModel>(x => x.HasKey(v => v.Key));
#pragma warning restore 1591
    }
}

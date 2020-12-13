using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents a registration token that can be used to register on the server.
    /// </summary>
    /// <remarks>
    /// The key is itself the registration token.
    /// </remarks>
    public class RegistrationToken : Model<string>
    {
        /// <summary>
        /// User who created this token.
        /// </summary>
        public virtual UserModel Forger { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<RegistrationToken>(x => x.HasKey(v => v.Key));
#pragma warning restore 1591
    }
}

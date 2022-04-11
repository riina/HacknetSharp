using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.EF.Models
{
    /// <summary>
    /// Represents a registration token that can be used to register on the server.
    /// </summary>
    /// <remarks>
    /// The key is itself the registration token.
    /// </remarks>
    public class RegistrationToken : EFModelHelper
    {
        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<HacknetSharp.Server.Models.RegistrationToken>(x => x.HasKey(v => v.Key));
#pragma warning restore 1591
    }
}

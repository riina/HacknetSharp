using System.Diagnostics.CodeAnalysis;
using HacknetSharp.Server.Common;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.BasicLogin
{
    public class RegistrationToken : Model<string>
    {
        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<RegistrationToken>(x => x.HasKey(v => v.Key));
#pragma warning restore 1591
    }
}

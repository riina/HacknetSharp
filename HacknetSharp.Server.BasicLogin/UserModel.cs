using System.Diagnostics.CodeAnalysis;
using HacknetSharp.Server.Common;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.BasicLogin
{
    public class UserModel : Model<string>
    {
        public string Base64Salt { get; set; } = null!;
        public string Base64Password { get; set; } = null!;
        public bool Admin { get; set; }
        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<UserModel>(x => x.HasKey(v => v.Key));
#pragma warning restore 1591
    }
}

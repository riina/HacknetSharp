using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Common.Models
{
    public class LoginModel : Model<Guid>
    {
        public virtual WorldModel World { get; set; } = null!;
        public virtual SystemModel System { get; set; } = null!;
        public virtual PersonModel? Person { get; set; }
        public Guid PersonForeignKey { get; set; }
        public virtual string User { get; set; } = null!;
        public virtual byte[] Hash { get; set; } = null!;
        public virtual byte[] Salt { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<LoginModel>(x => x.HasKey(v => v.Key));
#pragma warning restore 1591
    }
}

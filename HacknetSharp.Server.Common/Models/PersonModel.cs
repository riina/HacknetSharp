using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Common.Models
{
    public class PersonModel : WorldMember<Guid>
    {
        public string Name { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public List<SystemModel> Systems { get; set; } = null!;

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<PersonModel>(x => x.HasKey(v => v.Key));
#pragma warning restore 1591
    }
}

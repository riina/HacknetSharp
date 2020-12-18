using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents a person who owns systems.
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class PersonModel : WorldMember<Guid>
    {
        /// <summary>
        /// Proper name.
        /// </summary>
        public virtual string Name { get; set; } = null!;

        /// <summary>
        /// Username.
        /// </summary>
        public virtual string UserName { get; set; } = null!;

        /// <summary>
        /// Associated player user, if applicable.
        /// </summary>
        public virtual UserModel? User { get; set; }

        /// <summary>
        /// Primary system owned by the user.
        /// </summary>
        public virtual Guid DefaultSystem { get; set; }

        /// <summary>
        /// Sequence of active shells for the user, from oldest to newest.
        /// </summary>
        public List<ShellProcess> ShellChain { get; set; } = new();

        /// <summary>
        /// If true, this user has completed registration.
        /// </summary>
        public virtual bool StartedUp { get; set; }

        /// <summary>
        /// Set of all systems owned by this user.
        /// </summary>
        public virtual HashSet<SystemModel> Systems { get; set; } = null!;

        /// <summary>
        /// Reboot duration in seconds.
        /// </summary>
        public virtual double RebootDuration { get; set; }

        /// <summary>
        /// System disk capacity.
        /// </summary>
        public virtual int DiskCapacity { get; set; }

        /// <summary>
        /// System memory (bytes).
        /// </summary>
        public virtual long SystemMemory { get; set; }

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<PersonModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(y => y.Systems).WithOne(z => z.Owner).OnDelete(DeleteBehavior.Cascade);
                x.Ignore(v => v.ShellChain);
            });
#pragma warning restore 1591
    }
}

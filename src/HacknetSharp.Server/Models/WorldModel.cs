﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents an instance of a world in a server.
    /// </summary>
    public class WorldModel : Model<Guid>
    {
        /// <summary>
        /// Name of the world.
        /// </summary>
        public virtual string Name { get; set; } = null!;

        /// <summary>
        /// Label for the server.
        /// </summary>
        public virtual string Label { get; set; } = null!;

        /// <summary>
        /// Template for new players.
        /// </summary>
        public virtual string PlayerSystemTemplate { get; set; } = null!;

        /// <summary>
        /// Command line for new players.
        /// </summary>
        public virtual string StartupCommandLine { get; set; } = null!;

        /// <summary>
        /// Address range for new players.
        /// </summary>
        public virtual string PlayerAddressRange { get; set; } = null!;

        /// <summary>
        /// Active identities, both users and NPCs.
        /// </summary>
        public virtual HashSet<PersonModel> Persons { get; set; } = null!;

        /// <summary>
        /// Active systems.
        /// </summary>
        public virtual HashSet<SystemModel> Systems { get; set; } = null!;

        /// <summary>
        /// Default reboot duration in seconds.
        /// </summary>
        public virtual double RebootDuration { get; set; }

        /// <summary>
        /// System disk capacity.
        /// </summary>
        public virtual int DiskCapacity { get; set; }

        /// <summary>
        /// Current world time.
        /// </summary>
        public virtual double Now { get; set; }

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<WorldModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(y => y.Persons).WithOne(z => z.World!).OnDelete(DeleteBehavior.Cascade);
                x.HasMany(y => y.Systems).WithOne(z => z.World!).OnDelete(DeleteBehavior.Cascade);
            });
#pragma warning restore 1591
    }
}

using System;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents a directed knowledge connection from one system to another.
    /// </summary>
    public class KnownSystemModel : WorldMember<Guid>
    {
        /// <summary>
        /// Property required for EF database.
        /// </summary>
        public Guid FromKey { get; set; }

        /// <summary>
        /// Property required for EF database.
        /// </summary>
        public Guid ToKey { get; set; }

        /// <summary>
        /// System that knows the other system.
        /// </summary>
        public virtual SystemModel From { get; set; } = null!;

        /// <summary>
        /// System that is known by the other system.
        /// </summary>
        public virtual SystemModel To { get; set; } = null!;

        /// <summary>
        /// If true, represents a local connection.
        /// </summary>
        public bool Local { get; set; }
    }
}

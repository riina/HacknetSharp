using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Design;

namespace HacknetSharp.Server
{
    /// <inheritdoc />
    public abstract class ServerDatabaseContextFactoryBase : IDesignTimeDbContextFactory<ServerDatabaseContext>
    {
        /// <summary>
        /// Obtains program types desired for the database context.
        /// </summary>
        /// <returns>Program types.</returns>
        public abstract IEnumerable<Type> Programs { get; }

        /// <summary>
        /// Obtains service types desired for the database context.
        /// </summary>
        /// <returns>Service types.</returns>
        public abstract IEnumerable<Type> Services { get; }

        /// <summary>
        /// Obtains service types desired for the database context.
        /// </summary>
        /// <returns>Service types.</returns>
        public abstract IEnumerable<Type> Models { get; }

        /// <summary>
        /// Obtains custom program types as a group of groups.
        /// </summary>
        /// <returns>Group of groups of program types.</returns>
        protected abstract IEnumerable<IEnumerable<Type>> CustomPrograms { get; }

        /// <summary>
        /// Obtains custom service types as a group of groups.
        /// </summary>
        /// <returns>Group of groups of service types.</returns>
        protected abstract IEnumerable<IEnumerable<Type>> CustomServices { get; }

        /// <summary>
        /// Obtains custom model types as a group of groups.
        /// </summary>
        /// <returns>Group of groups of program types.</returns>
        protected abstract IEnumerable<IEnumerable<Type>> CustomModels { get; }

        /// <inheritdoc />
        public abstract ServerDatabaseContext CreateDbContext(string[] args);
    }
}

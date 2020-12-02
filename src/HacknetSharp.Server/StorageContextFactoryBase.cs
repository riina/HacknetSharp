using System;
using System.Collections.Generic;
using System.Linq;
using HacknetSharp.Server.Common;
using Microsoft.EntityFrameworkCore.Design;

namespace HacknetSharp.Server
{
    /// <inheritdoc />
    public abstract class StorageContextFactoryBase : IDesignTimeDbContextFactory<ServerStorageContext>
    {
        /// <summary>
        /// Obtains program types desired for the storage context.
        /// </summary>
        /// <returns>Program types.</returns>
        public virtual IEnumerable<Type> Programs =>
            ServerUtil.DefaultPrograms.Concat(CustomPrograms.SelectMany(e => e));

        /// <summary>
        /// Obtains service types desired for the storage context.
        /// </summary>
        /// <returns>Service types.</returns>
        public virtual IEnumerable<Type> Services =>
            ServerUtil.DefaultServices.Concat(CustomServices.SelectMany(e => e));

        /// <summary>
        /// Obtains custom program types as a group of groups.
        /// </summary>
        /// <returns>Group of groups of program types.</returns>
        protected virtual IEnumerable<IEnumerable<Type>> CustomPrograms => new[] {Executor._customPrograms};

        /// <summary>
        /// Obtains custom service types as a group of groups.
        /// </summary>
        /// <returns>Group of groups of service types.</returns>
        protected virtual IEnumerable<IEnumerable<Type>> CustomServices => new[] {Executor._customServices};

        /// <summary>
        /// Obtains custom model types as a group of groups.
        /// </summary>
        /// <returns>Group of groups of program types.</returns>
        protected virtual IEnumerable<IEnumerable<Type>> CustomModels => Enumerable.Empty<IEnumerable<Type>>();

        /// <inheritdoc />
        public abstract ServerStorageContext CreateDbContext(string[] args);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Obtains custom program types as a group of groups.
        /// </summary>
        /// <returns>Group of groups of program types.</returns>
        protected virtual IEnumerable<IEnumerable<Type>> CustomPrograms =>
            ServerUtil.LoadProgramTypesFromFolder(ServerConstants.ExtensionsFolder);

        /// <summary>
        /// Obtains custom model types as a group of groups.
        /// </summary>
        /// <returns>Group of groups of program types.</returns>
        protected virtual IEnumerable<IEnumerable<Type>> CustomModels => Enumerable.Empty<IEnumerable<Type>>();

        /// <inheritdoc />
        public abstract ServerStorageContext CreateDbContext(string[] args);
    }
}

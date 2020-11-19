using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore.Design;

namespace HacknetSharp.Server
{
    /// <inheritdoc />
    public abstract class StorageContextFactoryBase : IDesignTimeDbContextFactory<WorldStorageContext>
    {
        /// <summary>
        /// Obtains model types desired for the storage context.
        /// </summary>
        /// <returns>Model types.</returns>
        public virtual IEnumerable<Type> Models => Util.DefaultModels.Concat(CustomModels);

        /// <summary>
        /// Obtains model component types as a group.
        /// </summary>
        /// <returns>Group of model types.</returns>
        public virtual IEnumerable<Type> CustomModels =>
            CustomModelsIndividual.Concat(CustomModelsMulti.SelectMany(e => e));

        /// <summary>
        /// Obtains custom model types as a group.
        /// </summary>
        /// <returns>Group of model types.</returns>
        protected virtual IEnumerable<Type> CustomModelsIndividual => Enumerable.Empty<Type>();

        /// <summary>
        /// Obtains custom model types as a group of groups.
        /// </summary>
        /// <returns>Group of groups of model types.</returns>
        protected virtual IEnumerable<IEnumerable<Type>> CustomModelsMulti => Enumerable.Empty<IEnumerable<Type>>();

        /// <inheritdoc />
        public abstract WorldStorageContext CreateDbContext(string[] args);
    }
}

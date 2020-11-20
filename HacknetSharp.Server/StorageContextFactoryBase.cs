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
        /// Obtains program types desired for the storage context.
        /// </summary>
        /// <returns>Program types.</returns>
        public virtual IEnumerable<Type> Programs => ServerUtil.DefaultPrograms.Concat(CustomPrograms);

        /// <summary>
        /// Obtains program types as a group.
        /// </summary>
        /// <returns>Group of program types.</returns>
        public virtual IEnumerable<Type> CustomPrograms =>
            CustomProgramsIndividual.Concat(CustomProgramsMulti.SelectMany(e => e));

        /// <summary>
        /// Obtains custom program types as a group.
        /// </summary>
        /// <returns>Group of program types.</returns>
        protected virtual IEnumerable<Type> CustomProgramsIndividual => Enumerable.Empty<Type>();

        /// <summary>
        /// Obtains custom program types as a group of groups.
        /// </summary>
        /// <returns>Group of groups of program types.</returns>
        protected virtual IEnumerable<IEnumerable<Type>> CustomProgramsMulti => Enumerable.Empty<IEnumerable<Type>>();

        /// <inheritdoc />
        public abstract WorldStorageContext CreateDbContext(string[] args);
    }
}

using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HacknetSharp.Server.Sqlite
{
    /// <summary>
    /// Represents a factory for creating a Postgres-backed storage context.
    /// </summary>
    public abstract class SqliteStorageContextFactory : StorageContextFactoryBase
    {
        /// <summary>
        /// Environment variable for sqlite storage file
        /// </summary>
        public virtual string EnvStorageFile => "hndb_file";

        /// <summary>
        /// Assembly with migrations for the database
        /// </summary>
        public virtual Assembly MigrationAssembly => GetType().Assembly;

        /// <summary>
        /// If true, log queries to console
        /// </summary>
        public bool LogToConsole { get; set; }

        /// <inheritdoc />
        public override ServerStorageContext CreateDbContext(string[] args)
        {
            string file = Environment.GetEnvironmentVariable(EnvStorageFile) ??
                          throw new ApplicationException($"ENV {EnvStorageFile} not set");
            var ob = new DbContextOptionsBuilder<ServerStorageContext>();

            ob.UseSqlite($"Data Source={file};",
                b => b.MigrationsAssembly(MigrationAssembly.FullName));
            if (LogToConsole)
                ob.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            return new ServerStorageContext(ob.Options, Programs, CustomModels.SelectMany(e => e));
        }
    }
}

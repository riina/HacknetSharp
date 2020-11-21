using System;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HacknetSharp.Server.Postgres
{
    /// <summary>
    /// Represents a factory for creating a Postgres-backed storage context.
    /// </summary>
    public abstract class PostgresStorageContextFactory : StorageContextFactoryBase
    {
        /// <summary>
        /// Environment variable for postgres host.
        /// </summary>
        public virtual string EnvStorageHost => "hndb_host";

        /// <summary>
        /// Environment variable for postgres database name.
        /// </summary>
        public virtual string EnvStorageName => "hndb_name";

        /// <summary>
        /// Environment variable for postgres username.
        /// </summary>
        public virtual string EnvStorageUser => "hndb_user";

        /// <summary>
        /// Environment variable for postgres password.
        /// </summary>
        public virtual string EnvStoragePass => "hndb_pass";

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
            string host = Environment.GetEnvironmentVariable(EnvStorageHost) ??
                          throw new ApplicationException($"ENV {EnvStorageHost} not set");
            string db = Environment.GetEnvironmentVariable(EnvStorageName) ??
                        throw new ApplicationException($"ENV {EnvStorageName} not set");
            string user = Environment.GetEnvironmentVariable(EnvStorageUser) ??
                          throw new ApplicationException($"ENV {EnvStorageUser} not set");
            string pass = Environment.GetEnvironmentVariable(EnvStoragePass) ??
                          throw new ApplicationException($"ENV {EnvStoragePass} not set");
            var ob = new DbContextOptionsBuilder<ServerStorageContext>();

            ob.UseNpgsql($"Host={host};Database={db};Username={user};Password={pass}",
                b => b.MigrationsAssembly(MigrationAssembly.FullName));
            if (LogToConsole)
                ob.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            return new ServerStorageContext(ob.Options, Programs, CustomModels.SelectMany(e => e));
        }
    }
}

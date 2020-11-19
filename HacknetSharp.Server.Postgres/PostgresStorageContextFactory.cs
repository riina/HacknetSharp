using System;
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
        public virtual string EnvStorageHost => "cyr_storage_host";

        /// <summary>
        /// Environment variable for postgres database.
        /// </summary>
        public virtual string EnvStorageDb => "cyr_storage_db";

        /// <summary>
        /// Environment variable for postgres username.
        /// </summary>
        public virtual string EnvStorageUser => "cyr_storage_user";

        /// <summary>
        /// Environment variable for postgres password.
        /// </summary>
        public virtual string EnvStoragePass => "cyr_storage_pass";

        /// <summary>
        /// Assembly with migrations for the database
        /// </summary>
        public virtual Assembly MigrationAssembly => GetType().Assembly;

        /// <summary>
        /// If true, log queries to console
        /// </summary>
        public bool LogToConsole { get; set; }

        /// <inheritdoc />
        public override WorldStorageContext CreateDbContext(string[] args)
        {
            string host = Environment.GetEnvironmentVariable(EnvStorageHost) ??
                          throw new ApplicationException($"ENV {EnvStorageHost} not set");
            string db = Environment.GetEnvironmentVariable(EnvStorageDb) ??
                        throw new ApplicationException($"ENV {EnvStorageDb} not set");
            string user = Environment.GetEnvironmentVariable(EnvStorageUser) ??
                          throw new ApplicationException($"ENV {EnvStorageUser} not set");
            string pass = Environment.GetEnvironmentVariable(EnvStoragePass) ??
                          throw new ApplicationException($"ENV {EnvStoragePass} not set");
            var ob = new DbContextOptionsBuilder<WorldStorageContext>();

            ob.UseNpgsql($"Host={host};Database={db};Username={user};Password={pass}",
                b => b.MigrationsAssembly(MigrationAssembly.FullName));
            if (LogToConsole)
                ob.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            return new WorldStorageContext(ob.Options, Models);
        }
    }
}

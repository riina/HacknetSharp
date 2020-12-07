using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HacknetSharp.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace hss.Postgres
{
    /// <summary>
    /// Represents a factory for creating a Postgres-backed database context.
    /// </summary>
    public class PostgresContextFactory : ServerDatabaseContextFactoryBase
    {
        private static bool _fromMain;
        private ServerSettings? ServerYaml { get; init; }

        public static PostgresContextFactory CreateFactory(string[] args, ServerSettings? serverYaml)
        {
            _fromMain = true;
            return new PostgresContextFactory
            {
                ServerYaml = serverYaml, LogToConsole = serverYaml?.EnableLogging ?? false
            };
        }

        public override IEnumerable<Type> Programs =>
            ServerUtil.DefaultPrograms.Concat(CustomPrograms.SelectMany(e => e));

        public override IEnumerable<Type> Services =>
            ServerUtil.DefaultServices.Concat(CustomServices.SelectMany(e => e));

        public override IEnumerable<Type> Models =>
            ServerUtil.DefaultModels.Concat(CustomModels.SelectMany(e => e));

        protected override IEnumerable<IEnumerable<Type>> CustomPrograms => new[] {ServerUtil.CustomPrograms};

        protected override IEnumerable<IEnumerable<Type>> CustomServices => new[] {ServerUtil.CustomServices};

        protected override IEnumerable<IEnumerable<Type>> CustomModels => Enumerable.Empty<IEnumerable<Type>>();

        /// <summary>
        /// Environment variable for postgres host.
        /// </summary>
        public virtual string EnvStorageHost => "hndb_host";

        /// <summary>
        /// Environment variable for postgres database name.
        /// </summary>
        public virtual string EnvStorageDatabase => "hndb_name";

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
        public override ServerDatabaseContext CreateDbContext(string[] args)
        {
            string host = Environment.GetEnvironmentVariable(EnvStorageHost) ?? ServerYaml?.PostgresHost ?? (
                !_fromMain ? "kagura" : throw new ApplicationException($"ENV {EnvStorageHost} not set"));
            string database = Environment.GetEnvironmentVariable(EnvStorageDatabase) ?? ServerYaml?.PostgresDatabase ??
            (
                !_fromMain ? "sakaki" : throw new ApplicationException($"ENV {EnvStorageDatabase} not set"));
            string user = Environment.GetEnvironmentVariable(EnvStorageUser) ?? ServerYaml?.PostgresUser ?? (
                !_fromMain ? "tomo" : throw new ApplicationException($"ENV {EnvStorageUser} not set"));
            string pass = Environment.GetEnvironmentVariable(EnvStoragePass) ?? (
                !_fromMain ? "yomi" : throw new ApplicationException($"ENV {EnvStoragePass} not set"));
            var ob = new DbContextOptionsBuilder<ServerDatabaseContext>();

            ob.UseNpgsql($"Host={host};Database={database};Username={user};Password={pass}",
                b => b.MigrationsAssembly(MigrationAssembly.FullName));
            if (LogToConsole)
                ob.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            return new ServerDatabaseContext(ob.Options, Programs.Concat(Services), Models);
        }
    }
}

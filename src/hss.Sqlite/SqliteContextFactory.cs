using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HacknetSharp.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace hss.Sqlite
{
    /// <summary>
    /// Represents a factory for creating a SQLite-backed database context.
    /// </summary>
    public class SqliteContextFactory : ServerDatabaseContextFactoryBase
    {
        private static bool _fromMain;
        private ServerSettings? ServerYaml { get; init; }

        public static SqliteContextFactory CreateFactory(string[] args, ServerSettings? serverYaml)
        {
            _fromMain = true;
            return new SqliteContextFactory
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
        public override ServerDatabaseContext CreateDbContext(string[] args)
        {
            string file = Environment.GetEnvironmentVariable(EnvStorageFile) ?? ServerYaml?.SqliteFile ?? (
                !_fromMain ? "hakase" : throw new ApplicationException($"ENV {EnvStorageFile} not set"));
            var ob = new DbContextOptionsBuilder<ServerDatabaseContext>();

            ob.UseSqlite($"Data Source={file};",
                b => b.MigrationsAssembly(MigrationAssembly.FullName));
            if (LogToConsole)
                ob.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            return new ServerDatabaseContext(ob.Options, Programs.Concat(Services), Models);
        }
    }
}

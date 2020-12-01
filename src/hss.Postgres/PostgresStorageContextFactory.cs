﻿using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HacknetSharp.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace hss.Postgres
{
    /// <summary>
    /// Represents a factory for creating a Postgres-backed storage context.
    /// </summary>
    public class PostgresStorageContextFactory : StorageContextFactoryBase
    {
        private static bool _fromMain;
        private ServerYaml? ServerYaml { get; init; }

        public static async Task<int> Main(string[] args, ServerYaml? serverYaml)
        {
            _fromMain = true;
            return await new Executor(new PostgresStorageContextFactory
            {
                ServerYaml = serverYaml, LogToConsole = serverYaml?.EnableLogging ?? false
            }).Execute(args);
        }

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
        public override ServerStorageContext CreateDbContext(string[] args)
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
            var ob = new DbContextOptionsBuilder<ServerStorageContext>();

            ob.UseNpgsql($"Host={host};Database={database};Username={user};Password={pass}",
                b => b.MigrationsAssembly(MigrationAssembly.FullName));
            if (LogToConsole)
                ob.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            return new ServerStorageContext(ob.Options, Programs, CustomModels.SelectMany(e => e));
        }
    }
}
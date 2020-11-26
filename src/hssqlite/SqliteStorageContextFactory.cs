using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HacknetSharp.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace hssqlite
{
    /// <summary>
    /// Represents a factory for creating a SQLite-backed storage context.
    /// </summary>
    public class SqliteStorageContextFactory : StorageContextFactoryBase
    {
        private static bool _fromMain;

        public static async Task<int> Main(string[] args)
        {
            _fromMain = true;
            return await new Executor<SqliteStorageContextFactory>().Execute(args);
        }

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
            string file = Environment.GetEnvironmentVariable(EnvStorageFile) ?? (
                !_fromMain ? "hakase" : throw new ApplicationException($"ENV {EnvStorageFile} not set"));
            var ob = new DbContextOptionsBuilder<ServerStorageContext>();

            ob.UseSqlite($"Data Source={file};",
                b => b.MigrationsAssembly(MigrationAssembly.FullName));
            if (LogToConsole)
                ob.UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()));
            return new ServerStorageContext(ob.Options, Programs, CustomModels.SelectMany(e => e));
        }
    }
}

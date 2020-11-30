using System;
using System.IO;
using System.Threading.Tasks;
using HacknetSharp;
using HacknetSharp.Server;
using hss.Postgres;
using hss.Sqlite;

namespace hss
{
    internal static class Program
    {
        /// <summary>
        /// Environment variable for storage kind
        /// </summary>
        private const string EnvStorageKind = "hndb_kind";

        private static async Task<int> Main(string[] args)
        {
            ServerYaml? serverYaml = null;
            if (File.Exists(ServerConstants.ServerYamlFile))
                (_, serverYaml) = ServerUtil.ReadFromFile<ServerYaml>(ServerConstants.ServerYamlFile);
            string kind = Environment.GetEnvironmentVariable(EnvStorageKind) ?? serverYaml?.DatabaseKind ??
                throw new ApplicationException($"ENV {EnvStorageKind} not set");
            return await (kind.ToLowerInvariant() switch
            {
                "postgres" => PostgresStorageContextFactory.Main(args, serverYaml),
                "sqlite" => SqliteStorageContextFactory.Main(args, serverYaml),
                _ => throw new ApplicationException($"Unknown storage kind {kind}, need sqlite or postgres")
            }).Caf();
        }
    }
}

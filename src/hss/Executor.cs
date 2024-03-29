﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp;
using HacknetSharp.Server;
using HacknetSharp.Server.EF;
using hss.Postgres;
using hss.Runnables;
using hss.Sqlite;

namespace hss
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    public class Executor
    {
        /// <summary>
        /// Environment variable for storage kind
        /// </summary>
        private const string EnvStorageKind = "hndb_kind";

        private static async Task<int> Main(string[] args)
        {
            ServerSettings? serverYaml = null;
            if (File.Exists(HssConstants.ServerYamlFile))
                serverYaml = HssUtil.DefaultContentImporterGroup.ImportNotNull<ServerSettings>(HssConstants.ServerYamlFile);
            string? kind = Environment.GetEnvironmentVariable(EnvStorageKind);
            if (kind == null && serverYaml?.Database != null &&
                serverYaml.Database.TryGetValue("Kind", out var databaseKind))
                kind = databaseKind;
            kind ??= "sqlite";
            return await new Executor(kind.ToLowerInvariant() switch
            {
                "postgres" => PostgresContextFactory.CreateFactory(args, serverYaml),
                "sqlite" => SqliteContextFactory.CreateFactory(args, serverYaml),
                _ => throw new ApplicationException($"Unknown storage kind {kind}, need sqlite or postgres")
            }).Execute(args).Caf();
        }

        public ServerDatabaseContextFactoryBase ServerDatabaseContextFactory { get; }

        public Executor(ServerDatabaseContextFactoryBase serverDatabaseContextFactory)
        {
            ServerDatabaseContextFactory = serverDatabaseContextFactory;
            CustomPrograms = new HashSet<Type>();
            CustomServices = new HashSet<Type>();
        }

        internal interface IRunnable
        {
            Task<int> Run(Executor executor, IEnumerable<string> args);
        }

        internal interface ISelfRunnable
        {
            Task<int> Run(Executor executor);
        }

        public HashSet<Type> CustomServices { get; set; }
        public HashSet<Type> CustomPrograms { get; set; }

        public async Task<int> Execute(string[] args) => await Parser.Default
            .ParseArguments<RunCert, RunDatabase, RunUser, RunWorld, RunToken, RunNew, RunServe>(args.Take(1))
            .MapResult<IRunnable, Task<int>>(x => x.Run(this, args.Skip(1)), _ => Task.FromResult(1)).Caf();
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp;
using HacknetSharp.Server;
using Microsoft.EntityFrameworkCore;

namespace hss.Runnables
{
    [Verb("database", HelpText = "Manage database.")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    internal class RunDatabase : Executor.IRunnable
    {
        [Verb("update", HelpText = "Update database.")]
        private class Update : Executor.ISelfRunnable
        {
            [Option('y', HelpText = "Bypass warning.")]
            public bool Yes { get; set; }

            public async Task<int> Run(Executor executor)
            {
                var factory = executor.ServerDatabaseContextFactory;
                await using var ctx = factory.CreateDbContext(Array.Empty<string>());
                if (!Yes && !Util.Confirm("This operation is a blackbox and may cause data loss.\n" +
                                          "Manual migrations are recommended, see the below link for details:\n" +
                                          "https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying\n" +
                                          "Are you sure you want to apply applicable database migrations?")) return 0;
                Console.Write("Applying migrations... ");
                await ctx.Database.MigrateAsync();
                Console.WriteLine("Done.");
                return 0;
            }
        }

        private class Dummy
        {
        }

        public async Task<int> Run(Executor executor, IEnumerable<string> args) =>
            await Parser.Default.ParseArguments<Update, Dummy>(args)
                .MapResult<Executor.ISelfRunnable, Task<int>>(x => x.Run(executor),
                    _ => Task.FromResult(1)).Caf();
    }
}

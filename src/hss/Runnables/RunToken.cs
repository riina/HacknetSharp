using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp;
using HacknetSharp.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace hss.Runnables
{
    [Verb("token", HelpText = "Manage tokens.")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    internal class RunToken : Executor.IRunnable
    {
        private class CommonOptions
        {
            [Value(0, MetaName = "names", HelpText = "Forger names.")]
            public IEnumerable<string> Names { get; set; } = null!;
        }

        [Verb("remove", HelpText = "Remove entries.")]
        private class Remove : CommonOptions, Executor.ISelfRunnable
        {
            [Option('a', "all", HelpText = "Remove all tokens.")]
            public bool All { get; set; }

            public async Task<int> Run(Executor executor)
            {
                var factory = executor.ServerDatabaseContextFactory;
                await using var ctx = factory.CreateDbContext(Array.Empty<string>());
                var names = new HashSet<string>(Names);
                var tokens = await (All
                    ? ctx.Set<RegistrationToken>()
                    : ctx.Set<RegistrationToken>().Where(u => names.Contains(u.Forger.Key))).ToListAsync().Caf();

                foreach (var token in tokens)
                    Console.WriteLine($"{token.Forger.Key}");

                if (!Util.Confirm("Are you sure you want to proceed with deletion?")) return 0;

                ctx.RemoveRange(tokens);
                await ctx.SaveChangesAsync().Caf();
                return 0;
            }
        }

        private class List : CommonOptions, Executor.ISelfRunnable
        {
            [Option('a', "all", HelpText = "List all tokens.")]
            public bool All { get; set; }

            public async Task<int> Run(Executor executor)
            {
                var factory = executor.ServerDatabaseContextFactory;
                await using var ctx = factory.CreateDbContext(Array.Empty<string>());
                var names = new HashSet<string>(Names);
                var tokens = await (All
                    ? ctx.Set<RegistrationToken>()
                    : ctx.Set<RegistrationToken>().Where(u => names.Contains(u.Forger.Key))).ToListAsync().Caf();

                foreach (var token in tokens)
                    Console.WriteLine($"{token.Forger.Key}");

                return 0;
            }
        }


        public async Task<int> Run(Executor executor, IEnumerable<string> args) =>
            await Parser.Default.ParseArguments<Remove, List>(args)
                .MapResult<Executor.ISelfRunnable, Task<int>>(x => x.Run(executor),
                    x => Task.FromResult(1)).Caf();
    }
}

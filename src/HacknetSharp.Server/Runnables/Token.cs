using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Runnables
{
    [Verb("token", HelpText = "Manage tokens.")]
    internal class Token<TDatabaseFactory> : Executor<TDatabaseFactory>.IRunnable
        where TDatabaseFactory : StorageContextFactoryBase
    {
        private class CommonOptions
        {
            [Value(0, MetaName = "names", HelpText = "Forger names.")]
            public IEnumerable<string> Names { get; set; } = null!;
        }

        [Verb("remove", HelpText = "Remove entries.")]
        private class Remove : CommonOptions, Executor<TDatabaseFactory>.ISelfRunnable
        {
            [Option('a', "all", HelpText = "Remove all tokens.")]
            public bool All { get; set; }

            public async Task<int> Run(Executor<TDatabaseFactory> executor)
            {
                var factory = Activator.CreateInstance<TDatabaseFactory>();
                await using var ctx = factory.CreateDbContext(Array.Empty<string>());
                var names = new HashSet<string>(Names);
                var tokens = await (All
                    ? ctx.Set<RegistrationToken>()
                    : ctx.Set<RegistrationToken>().Where(u => names.Contains(u.Forger.Key))).ToListAsync().Caf();
                ctx.RemoveRange(tokens);
                await ctx.SaveChangesAsync().Caf();
                return 0;
            }
        }

        private class List : CommonOptions, Executor<TDatabaseFactory>.ISelfRunnable
        {
            [Option('a', "all", HelpText = "List all tokens.")]
            public bool All { get; set; }

            public async Task<int> Run(Executor<TDatabaseFactory> executor)
            {
                var factory = Activator.CreateInstance<TDatabaseFactory>();
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


        public async Task<int> Run(Executor<TDatabaseFactory> executor, IEnumerable<string> args) =>
            await Parser.Default.ParseArguments<Remove, List>(args)
                .MapResult<Executor<TDatabaseFactory>.ISelfRunnable, Task<int>>(x => x.Run(executor),
                    x => Task.FromResult(1)).Caf();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp.Server.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Runnables
{
    [Verb("world", HelpText = "Manage worlds.")]
    internal class World<TDatabaseFactory> : Executor<TDatabaseFactory>.IRunnable
        where TDatabaseFactory : StorageContextFactoryBase
    {
        [Verb("create", HelpText = "Create world.")]
        private class Create : Executor<TDatabaseFactory>.ISelfRunnable
        {
            [Value(0, MetaName = "name", HelpText = "World name.", Required = true)]
            public string Name { get; set; } = null!;

            [Value(1, MetaName = "template", HelpText = "World template name.", Required = true)]
            public string Template { get; set; } = null!;

            public async Task<int> Run(Executor<TDatabaseFactory> executor)
            {
                var factory = Activator.CreateInstance<TDatabaseFactory>();
                await using var ctx = factory.CreateDbContext(Array.Empty<string>());

                var xizt = await ctx.FindAsync<WorldModel>(Name).Caf();
                if (xizt != null)
                {
                    Console.WriteLine("A world with specified name already exists.");
                    return 0;
                }

                var templates = ServerUtil.GetTemplates("");
                if (!templates.WorldTemplates.TryGetValue(Template, out var template))
                {
                    Console.WriteLine("Could not find a template with the specified name.");
                    return 89;
                }

                var world = new Spawn().World(Name, template);
                ctx.Add(world);
                await ctx.SaveChangesAsync().Caf();
                return 0;
            }
        }

        [Verb("remove", HelpText = "Remove worlds.")]
        private class Remove : Executor<TDatabaseFactory>.ISelfRunnable
        {
            [Value(0, MetaName = "names", HelpText = "World names.", Required = true)]
            public IEnumerable<string> Names { get; set; } = null!;

            [Option('a', "all", HelpText = "Remove all worlds.")]
            public bool All { get; set; }

            public async Task<int> Run(Executor<TDatabaseFactory> executor)
            {
                var factory = Activator.CreateInstance<TDatabaseFactory>();
                await using var ctx = factory.CreateDbContext(Array.Empty<string>());
                var names = new HashSet<string>(Names);

                var worlds = await (All
                    ? ctx.Set<WorldModel>()
                    : ctx.Set<WorldModel>().Where(u => names.Contains(u.Name))).ToListAsync().Caf();

                foreach (var world in worlds)
                    Console.WriteLine($"{world.Name}:{world.Key}");

                if (!Util.Confirm("Are you sure you want to proceed with deletion?")) return 0;

                ctx.RemoveRange(worlds);
                await ctx.SaveChangesAsync().Caf();
                return 0;
            }
        }

        private class List : Executor<TDatabaseFactory>.ISelfRunnable
        {
            [Value(0, MetaName = "names", HelpText = "World names.")]
            public IEnumerable<string> Names { get; set; } = null!;

            [Option('a', "all", HelpText = "List all worlds.")]
            public bool All { get; set; }

            public async Task<int> Run(Executor<TDatabaseFactory> executor)
            {
                var factory = Activator.CreateInstance<TDatabaseFactory>();
                await using var ctx = factory.CreateDbContext(Array.Empty<string>());
                var names = new HashSet<string>(Names);

                var worlds = await (All
                    ? ctx.Set<WorldModel>()
                    : ctx.Set<WorldModel>().Where(u => names.Contains(u.Name))).ToListAsync().Caf();

                foreach (var world in worlds)
                    Console.WriteLine($"{world.Name}:{world.Key}");
                return 0;
            }
        }

        public async Task<int> Run(Executor<TDatabaseFactory> executor, IEnumerable<string> args) =>
            await Parser.Default.ParseArguments<Create, Remove, List>(args)
                .MapResult<Executor<TDatabaseFactory>.ISelfRunnable, Task<int>>(x => x.Run(executor),
                    x => Task.FromResult(1)).Caf();
    }
}

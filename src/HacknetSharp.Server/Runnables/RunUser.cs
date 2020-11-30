using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp.Server.Common;
using HacknetSharp.Server.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Runnables
{
    [Verb("user", HelpText = "Manage users.")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    internal class RunUser : Executor.IRunnable
    {
        [Verb("create", HelpText = "Create user.")]
        private class Create : Executor.ISelfRunnable
        {
            [Value(0, MetaName = "name", HelpText = "User name.", Required = true)]
            public string Name { get; set; } = null!;

            [Option('a', "admin", HelpText = "Make user admin.")]
            public bool Admin { get; set; }

            public async Task<int> Run(Executor executor)
            {
                var factory = executor.StorageContextFactory;
                await using var ctx = factory.CreateDbContext(Array.Empty<string>());

                var xizt = await ctx.FindAsync<UserModel>(Name).Caf();
                if (xizt != null)
                {
                    Console.WriteLine("A user with specified name already exists.");
                    return 0;
                }

                string? pass = Util.PromptPassword("Pass:");
                if (pass == null) return 0;
                var (hash, salt) = CommonUtil.HashPassword(pass);
                ctx.Add(new UserModel {Admin = Admin, Hash = hash, Salt = salt, Key = Name});
                await ctx.SaveChangesAsync().Caf();
                return 0;
            }
        }

        [Verb("remove", HelpText = "Remove users.")]
        private class Remove : Executor.ISelfRunnable
        {
            [Value(0, MetaName = "names", HelpText = "User names.", Required = true)]
            public IEnumerable<string> Names { get; set; } = null!;

            [Option('a', "all", HelpText = "Remove all users.")]
            public bool All { get; set; }

            public async Task<int> Run(Executor executor)
            {
                var factory = executor.StorageContextFactory;
                await using var ctx = factory.CreateDbContext(Array.Empty<string>());
                var names = new HashSet<string>(Names);

                var users = await (All
                    ? ctx.Set<UserModel>()
                    : ctx.Set<UserModel>().Where(u => names.Contains(u.Key))).ToListAsync().Caf();

                foreach (var user in users)
                {
                    var player = await ctx.FindAsync<PlayerModel>(user.Key).Caf();
                    if (player != null)
                    {
                        foreach (var person in player.Identities)
                        {
                            foreach (var system in person.Systems) ctx.RemoveRange(system.Files);
                            ctx.RemoveRange(person.Systems);
                        }

                        ctx.RemoveRange(player.Identities);
                        ctx.Remove(player);
                    }
                }

                foreach (var user in users)
                    Console.WriteLine($"{user.Key}:{(user.Admin ? "admin" : "regular")}");

                if (!Util.Confirm("Are you sure you want to proceed with deletion?")) return 0;

                ctx.RemoveRange(users);
                await ctx.SaveChangesAsync().Caf();
                return 0;
            }
        }

        [Verb("list", HelpText = "List users.")]
        private class List : Executor.ISelfRunnable
        {
            [Value(0, MetaName = "names", HelpText = "User names.")]
            public IEnumerable<string> Names { get; set; } = null!;

            [Option('a', "all", HelpText = "List all users.")]
            public bool All { get; set; }

            public async Task<int> Run(Executor executor)
            {
                var factory = executor.StorageContextFactory;
                await using var ctx = factory.CreateDbContext(Array.Empty<string>());
                var names = new HashSet<string>(Names);

                var users = await (All
                    ? ctx.Set<UserModel>()
                    : ctx.Set<UserModel>().Where(u => names.Contains(u.Key))).ToListAsync().Caf();

                foreach (var user in users)
                    Console.WriteLine($"{user.Key}:{(user.Admin ? "admin" : "regular")}");
                return 0;
            }
        }

        public async Task<int> Run(Executor executor, IEnumerable<string> args) =>
            await Parser.Default.ParseArguments<Create, Remove, List>(args)
                .MapResult<Executor.ISelfRunnable, Task<int>>(x => x.Run(executor),
                    x => Task.FromResult(1)).Caf();
    }
}

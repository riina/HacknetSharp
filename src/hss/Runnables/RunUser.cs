using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp;
using HacknetSharp.Server;
using HacknetSharp.Server.EF;
using HacknetSharp.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace hss.Runnables
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
                var factory = executor.ServerDatabaseContextFactory;
                await using var ctx = factory.CreateDbContext(Array.Empty<string>());

                var xizt = await ctx.FindAsync<UserModel>(Name).Caf();
                if (xizt != null)
                {
                    Console.WriteLine("A user with specified name already exists.");
                    return 0;
                }

                Console.Write("Pass:");
                string? pass = Util.ReadPassword();
                if (pass == null) return 0;
                var (hash, salt) = ServerUtil.HashPassword(pass);
                var db = new ServerDatabase(ctx);
                new Spawn(db).User(Name, hash, salt, Admin);
                Console.Write("Committing database... ");
                await db.SyncAsync().Caf();
                Console.WriteLine("Done.");
                return 0;
            }
        }

        [Verb("remove", HelpText = "Remove users.")]
        private class Remove : Executor.ISelfRunnable
        {
            [Value(0, MetaName = "names", HelpText = "User names.")]
            public IEnumerable<string> Names { get; set; } = null!;

            [Option('a', "all", HelpText = "Remove all users.")]
            public bool All { get; set; }

            public async Task<int> Run(Executor executor)
            {
                var factory = executor.ServerDatabaseContextFactory;
                await using var ctx = factory.CreateDbContext(Array.Empty<string>());
                var names = new HashSet<string>(Names);

                var users = await (All
                    ? ctx.Set<UserModel>()
                    : ctx.Set<UserModel>().Where(u => names.Contains(u.Key))).ToListAsync().Caf();

                foreach (var user in users)
                    Console.WriteLine($"{user.Key}:{(user.Admin ? "admin" : "regular")}");

                if (!Util.Confirm("Are you sure you want to proceed with deletion?")) return 0;

                var db = new ServerDatabase(ctx);
                var spawn = new Spawn(db);
                foreach (var user in users) spawn.RemoveUser(user);
                Console.Write("Committing database... ");
                await db.SyncAsync().Caf();
                Console.WriteLine("Done.");
                return 0;
            }
        }

        [Verb("list", HelpText = "List users.")]
        private class List : Executor.ISelfRunnable
        {
            [Value(0, MetaName = "names", HelpText = "User names.")]
            public IEnumerable<string> Names { get; set; } = null!;

            public async Task<int> Run(Executor executor)
            {
                var factory = executor.ServerDatabaseContextFactory;
                await using var ctx = factory.CreateDbContext(Array.Empty<string>());
                var names = new HashSet<string>(Names);

                Console.Write("Retrieving from database... ");
                var users = await (names.Count == 0
                    ? ctx.Set<UserModel>()
                    : ctx.Set<UserModel>().Where(u => names.Contains(u.Key))).ToListAsync().Caf();
                Console.WriteLine("Done.");

                foreach (var user in users)
                    Console.WriteLine($"{user.Key}:{(user.Admin ? "admin" : "regular")}");
                return 0;
            }
        }

        public async Task<int> Run(Executor executor, IEnumerable<string> args) =>
            await Parser.Default.ParseArguments<Create, Remove, List>(args)
                .MapResult<Executor.ISelfRunnable, Task<int>>(x => x.Run(executor),
                    _ => Task.FromResult(1)).Caf();
    }
}

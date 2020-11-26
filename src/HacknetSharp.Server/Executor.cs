using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using HacknetSharp.Server.Runnables;

namespace HacknetSharp.Server
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    public class Executor<TDatabaseFactory> : Executor where TDatabaseFactory : StorageContextFactoryBase
    {
        internal interface IRunnable
        {
            Task<int> Run(Executor<TDatabaseFactory> executor, IEnumerable<string> args);
        }

        internal interface ISelfRunnable
        {
            Task<int> Run(Executor<TDatabaseFactory> executor);
        }

        public HashSet<Type[]> CustomPrograms { get; set; } = _customPrograms;

        public async Task<int> Execute(string[] args) => await Parser.Default
            .ParseArguments<Cert<TDatabaseFactory>, User<TDatabaseFactory>, Token<TDatabaseFactory>,
                New<TDatabaseFactory>, Serve<TDatabaseFactory>>(args.Take(1))
            .MapResult<IRunnable, Task<int>>(x => x.Run(this, args.Skip(1)), x => Task.FromResult(1)).Caf();
    }


    public class Executor
    {
        protected static readonly HashSet<Type[]> _customPrograms =
            ServerUtil.LoadProgramTypesFromFolder(ServerConstants.ExtensionsFolder);
    }
}

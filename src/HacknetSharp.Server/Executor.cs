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
    public class Executor
    {
        public StorageContextFactoryBase StorageContextFactory { get; }

        public Executor(StorageContextFactoryBase storageContextFactory)
        {
            StorageContextFactory = storageContextFactory;
        }

        protected static readonly HashSet<Type[]> _customPrograms =
            ServerUtil.LoadProgramTypesFromFolder(ServerConstants.ExtensionsFolder);

        internal interface IRunnable
        {
            Task<int> Run(Executor executor, IEnumerable<string> args);
        }

        internal interface ISelfRunnable
        {
            Task<int> Run(Executor executor);
        }

        public HashSet<Type[]> CustomPrograms { get; set; } = _customPrograms;

        public async Task<int> Execute(string[] args) => await Parser.Default
            .ParseArguments<RunCert, RunUser, RunWorld, RunToken, RunNew, RunServe>(args.Take(1))
            .MapResult<IRunnable, Task<int>>(x => x.Run(this, args.Skip(1)), x => Task.FromResult(1)).Caf();
    }
}

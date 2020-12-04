using System.Collections.Generic;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;

namespace hss.Core.CorePrograms
{
    [ProgramInfo("core:cd", "cd", "change working directory",
        "change current working directory to\n" +
        "specified directory or home directory", "[directory]", false)]
    public class CdProgram : Program
    {
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var system = context.System;
            var argv = context.Argv;
            if (argv.Length == 1) yield break;
            string path;
            try
            {
                path = GetNormalized(Combine(context.Person.WorkingDirectory, argv[1]));
            }
            catch
            {
                yield break;
            }

            if (path == "/")
                context.Person.WorkingDirectory = "/";
            else if (system.TryGetWithAccess(path, context.Login, out var result, out var closest))
                switch (closest.Kind)
                {
                    case FileModel.FileKind.TextFile:
                    case FileModel.FileKind.FileFile:
                    case FileModel.FileKind.ProgFile:
                        user.WriteEventSafe(Output($"cd: {path}: Is a file\n"));
                        user.FlushSafeAsync();
                        yield break;
                    case FileModel.FileKind.Folder:
                        context.Person.WorkingDirectory = path;
                        yield break;
                }
            else
                switch (result)
                {
                    case ReadAccessResult.Readable:
                        break;
                    case ReadAccessResult.NotReadable:
                        user.WriteEventSafe(Output($"cd: {path}: Permission denied\n"));
                        user.FlushSafeAsync();
                        yield break;
                    case ReadAccessResult.NoExist:
                        user.WriteEventSafe(Output($"cd: {path}: No such file or directory\n"));
                        user.FlushSafeAsync();
                        yield break;
                }
        }
    }
}

using System.Collections.Generic;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:cd", "cd", "change working directory",
        "change current working directory to\nspecified directory or home directory\n",
        "[directory]", false)]
    public class CdProgram : Program
    {
        /// <inheritdoc />
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
                path = GetNormalized(Combine(context.Shell.WorkingDirectory, argv[1]));
            }
            catch
            {
                yield break;
            }

            if (path == "/")
                context.Shell.WorkingDirectory = "/";
            else if (system.TryGetFile(path, context.Login, out var result, out var closestStr, out var closest))
                switch (closest.Kind)
                {
                    case FileModel.FileKind.TextFile:
                    case FileModel.FileKind.FileFile:
                    case FileModel.FileKind.ProgFile:
                        user.WriteEventSafe(Output($"cd: {path}: Is a file\n"));
                        user.FlushSafeAsync();
                        yield break;
                    case FileModel.FileKind.Folder:
                        context.Shell.WorkingDirectory = path;
                        yield break;
                }
            else
                switch (result)
                {
                    case ReadAccessResult.NotReadable:
                        user.WriteEventSafe(Output($"cd: {closestStr}: Permission denied\n"));
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

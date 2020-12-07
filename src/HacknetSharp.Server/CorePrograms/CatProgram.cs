using System.Collections.Generic;
using System.Linq;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:cat", "cat", "concatenate and print files",
        "print provided files sequentially in command-line order",
        "[files...]", false)]
    public class CatProgram : Program
    {
        private static readonly OutputEvent _newlineOutput = new OutputEvent {Text = "\n"};

        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var system = context.System;
            var argv = context.Argv;
            if (argv.Length == 1)
            {
                user.WriteEventSafe(Output("At least 1 operand is required by this command\n"));
                user.FlushSafeAsync();
                yield break;
            }

            foreach (var file in argv.Skip(1))
            {
                string path = GetNormalized(Combine(context.Shell.WorkingDirectory, file));
                if (path == "/")
                {
                    user.WriteEventSafe(Output($"cat: {path}: Is a directory\n"));
                    continue;
                }

                if (system.TryGetWithAccess(path, context.Login, out var result, out var closest))
                    switch (closest.Kind)
                    {
                        case FileModel.FileKind.TextFile:
                            user.WriteEventSafe(Output(closest.Content ?? ""));
                            user.WriteEventSafe(_newlineOutput);
                            break;
                        case FileModel.FileKind.FileFile:
                            user.WriteEventSafe(Output($"cat: {path}: Is a binary file\n"));
                            break;
                        case FileModel.FileKind.ProgFile:
                            user.WriteEventSafe(Output($"cat: {path}: Is a binary file\n"));
                            break;
                        case FileModel.FileKind.Folder:
                            user.WriteEventSafe(Output($"cat: {path}: Is a directory\n"));
                            break;
                    }
                else
                    switch (result)
                    {
                        case ReadAccessResult.Readable:
                            break;
                        case ReadAccessResult.NotReadable:
                            user.WriteEventSafe(Output($"cat: {path}: Permission denied\n"));
                            user.FlushSafeAsync();
                            yield break;
                        case ReadAccessResult.NoExist:
                            user.WriteEventSafe(Output($"cat: {path}: No such file or directory\n"));
                            user.FlushSafeAsync();
                            yield break;
                    }
            }

            user.FlushSafeAsync();
        }
    }
}

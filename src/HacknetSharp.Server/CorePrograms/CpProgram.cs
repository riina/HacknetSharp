using System.Collections.Generic;
using System.IO;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:cp", "cp", "copy files and directories",
        "copy source files to specified\n" +
        "destination", "<source>... <dest>", false)]
    public class CpProgram : Program
    {
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var system = context.System;
            var argv = context.Argv;
            if (argv.Length < 3)
            {
                user.WriteEventSafe(
                    Output("At least 2 operands are required by this command:\n\t<source>... <dest>\n"));
                user.FlushSafeAsync();
                yield break;
            }

            string target;
            try
            {
                target = GetNormalized(Combine(context.Shell.WorkingDirectory, argv[^1]));
            }
            catch
            {
                yield break;
            }

            var spawn = context.World.Spawn;
            var login = context.Login;
            foreach (var input in argv[1..^2])
            {
                string inputFmt = GetNormalized(input);
                if (system.TryGetWithAccess(inputFmt, login, out var result, out var closest))
                {
                    try
                    {
                        string lclTarget;
                        string lclName;
                        if (closest.Kind == FileModel.FileKind.Folder || argv.Length != 3)
                        {
                            lclTarget = target;
                            lclName = closest.Name;
                        }
                        else
                        {
                            lclTarget = GetDirectoryName(target) ?? "/";
                            lclName = GetFileName(target);
                        }

                        spawn.Duplicate(system, login, lclTarget, lclName, closest);
                    }
                    catch (IOException e)
                    {
                        user.WriteEventSafe(Output($"{e.Message}\n"));
                        user.FlushSafeAsync();
                        yield break;
                    }
                }
                else
                    switch (result)
                    {
                        case ReadAccessResult.Readable:
                            break;
                        case ReadAccessResult.NotReadable:
                            user.WriteEventSafe(Output($"{inputFmt}: Permission denied\n"));
                            user.FlushSafeAsync();
                            yield break;
                        case ReadAccessResult.NoExist:
                            user.WriteEventSafe(Output($"{inputFmt}: No such file or directory\n"));
                            user.FlushSafeAsync();
                            yield break;
                    }
            }
        }
    }
}

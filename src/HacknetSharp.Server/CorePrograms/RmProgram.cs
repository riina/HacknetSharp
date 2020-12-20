using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:rm", "rm", "remove files or directories",
        "remove specified files or directories",
        "<files>...", false)]
    public class RmProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var system = context.System;
            string[] argv = context.Argv;
            if (argv.Length < 2)
            {
                user.WriteEventSafe(Output("At least 1 operand is required by this command:\n\t<file>...\n"));
                user.FlushSafeAsync();
                yield break;
            }

            try
            {
                var spawn = context.World.Spawn;
                var login = context.Login;
                string workDir = context.Shell.WorkingDirectory;
                foreach (var input in argv[1..])
                {
                    string inputFmt = GetNormalized(Combine(workDir, input));
                    if (system.TryGetFile(inputFmt, login, out var result, out var closestStr, out var closest))
                    {
                        try
                        {
                            string fp = closest.FullPath;
                            if (closest.Kind == FileModel.FileKind.Folder && system.Files.Any(f => f.Path == fp))
                            {
                                user.WriteEventSafe(Output($"{inputFmt}: Directory not empty\n"));
                                user.FlushSafeAsync();
                                yield break;
                            }

                            spawn.RemoveFile(closest, login);
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
                            case ReadAccessResult.NotReadable:
                                user.WriteEventSafe(Output($"{closestStr}: Permission denied\n"));
                                user.FlushSafeAsync();
                                yield break;
                            case ReadAccessResult.NoExist:
                                user.WriteEventSafe(Output($"{inputFmt}: No such file or directory\n"));
                                user.FlushSafeAsync();
                                yield break;
                        }
                }
            }
            catch (Exception e)
            {
                user.WriteEventSafe(Output($"{e.Message}\n"));
                user.FlushSafeAsync();
            }
        }
    }
}

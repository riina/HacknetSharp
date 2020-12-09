using System;
using System.Collections.Generic;
using System.IO;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:rm", "rm", "remove files or directories",
        "remove specified files or directories",
        "<files>...", false)]
    public class RmProgram : Program
    {
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var system = context.System;
            var argv = context.Argv;
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
                    if (system.TryGetWithAccess(inputFmt, login, out var result, out var closest))
                    {
                        try
                        {
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
            catch (Exception e)
            {
                user.WriteEventSafe(Output($"{e.Message}\n"));
                user.FlushSafeAsync();
            }
        }
    }
}

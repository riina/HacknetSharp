using System;
using System.Collections.Generic;
using System.IO;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:mkdir", "mkdir", "make directories",
        "create directories if they don't exist",
        "<dir>...", false)]
    public class MkdirProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var system = context.System;
            var argv = context.Argv;
            if (argv.Length < 2)
            {
                user.WriteEventSafe(Output("At least 1 operand is required by this command:\n\t<dir>...\n"));
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
                    if (inputFmt == "/") continue;
                    system.TryGetFile(inputFmt, login, out var result, out var closestStr, out _);
                    {
                        switch (result)
                        {
                            case ReadAccessResult.Readable:
                                user.WriteEventSafe(Output($"{inputFmt}: Path exists\n"));
                                user.FlushSafeAsync();
                                yield break;
                            case ReadAccessResult.NotReadable:
                                user.WriteEventSafe(Output($"{closestStr}: Permission denied\n"));
                                user.FlushSafeAsync();
                                yield break;
                            case ReadAccessResult.NoExist:
                                try
                                {
                                    var (path, name) = GetDirectoryAndName(inputFmt);
                                    spawn.Folder(system, login, name, path);
                                }
                                catch (IOException e)
                                {
                                    user.WriteEventSafe(Output($"{e.Message}\n"));
                                    user.FlushSafeAsync();
                                    yield break;
                                }

                                break;
                        }
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

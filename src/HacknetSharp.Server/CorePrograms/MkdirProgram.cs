using System.Collections.Generic;
using System.IO;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:mkdir", "mkdir", "make directories",
        "create directories if they don't exist", "<dir>...", false)]
    public class MkdirProgram : Program
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
                user.WriteEventSafe(Output("At least 1 operand is required by this command:\n\t<dir>... <dest>\n"));
                user.FlushSafeAsync();
                yield break;
            }

            var spawn = context.World.Spawn;
            var login = context.Login;
            foreach (var input in argv[1..^1])
            {
                string inputFmt = GetNormalized(input);
                if(inputFmt == "/") continue;
                system.TryGetWithAccess(inputFmt, login, out var result, out _);
                {
                    switch (result) {
                        case ReadAccessResult.Readable:
                            user.WriteEventSafe(Output($"{inputFmt}: Path exists\n"));
                            user.FlushSafeAsync();
                            yield break;
                        case ReadAccessResult.NotReadable:
                            user.WriteEventSafe(Output($"{inputFmt}: Permission denied\n"));
                            user.FlushSafeAsync();
                            yield break;
                        case ReadAccessResult.NoExist:
                            try
                            {
                                var (path, name) = SystemModel.GetDirectoryAndName(inputFmt);
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
    }
}

using System.Collections.Generic;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:cd", "change working directory",
        "change current working directory to\n" +
        "specified directory or home directory\n\n" +
        "cd [directory]")]
    public class CdProgram : Program
    {
        public override IEnumerator<YieldToken?> Invoke(CommandContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(CommandContext context)
        {
            var user = context.PersonContext;
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

            if (system.FileExists(path))
            {
                user.WriteEventSafe(Output($"cd: {path}: Is a file\n"));
                user.FlushSafeAsync();
                yield break;
            }

            if (!system.DirectoryExists(path))
            {
                user.WriteEventSafe(Output($"cd: {path}: No such file or directory\n"));
                user.FlushSafeAsync();
                yield break;
            }

            context.Person.WorkingDirectory = path;
            user.FlushSafeAsync();
        }
    }
}

using System.Collections.Generic;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:ls", "list directory contents",
        "list contents of specified directory\n" +
        "or current working directory\n\n" +
        "ls [directory]")]
    public class LsProgram : Program
    {
        public override IEnumerator<YieldToken?> Invoke(CommandContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(CommandContext context)
        {
            var user = context.PersonContext;
            if (!user.Connected) yield break;
            var system = context.System;
            var argv = context.Argv;
            string path = argv.Length > 1 ? argv[1] : context.Person.WorkingDirectory;
            if (!system.DirectoryExists(path))
            {
                user.WriteEventSafe(Output($"ls: {path}: No such file or directory\n"));
                user.FlushSafeAsync();
                yield break;
            }

            foreach (var entry in system.EnumerateDirectory(path)) user.WriteEventSafe(Output($"{entry.Name}\n"));

            user.FlushSafeAsync();
        }
    }
}

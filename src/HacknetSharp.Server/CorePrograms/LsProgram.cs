﻿using System.Collections.Generic;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:ls", "list directory contents",
        "list contents of specified directory\r\n" +
        "or current working directory" +
        "ls [directory]")]
    public class LsProgram : Program
    {
        public override IEnumerator<YieldToken?> Invoke(CommandContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(CommandContext context)
        {
            var system = context.System;
            var user = context.Person;
            var argv = context.Argv;
            if (!user.Connected) yield break;
            string path = argv.Length > 1 ? argv[1] : user.GetPerson(context.World).WorkingDirectory;
            if (!system.DirectoryExists(path))
            {
                user.WriteEventSafe(Output($"ls: {path}: No such file or directory\n"));
                user.FlushSafeAsync();
                yield break;
            }

            foreach (var entry in system.EnumerateDirectory(path))
            {
                user.WriteEventSafe(Output(entry.Path == $"/{entry.Name}" ? "" : $"{entry.Path}/{entry.Name}"));
            }

            user.FlushSafeAsync();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            var user = context.User;
            if (!user.Connected) yield break;
            var system = context.System;
            var argv = context.Argv;
            string path;
            try
            {
                path = argv.Length > 1
                    ? Combine(context.Person.WorkingDirectory, argv[1])
                    : context.Person.WorkingDirectory;
            }
            catch
            {
                yield break;
            }

            if (!system.DirectoryExists(path))
            {
                user.WriteEventSafe(Output($"ls: {path}: No such file or directory\n"));
                user.FlushSafeAsync();
                yield break;
            }

            if (!system.CanRead(path, context.Login))
            {
                user.WriteEventSafe(Output($"ls: {path}: Permission denied\n"));
                user.FlushSafeAsync();
                yield break;
            }

            StringBuilder sb = new StringBuilder();

            var fileList = new List<string>(system.EnumerateDirectory(path).Select(f => f.Name));
            if (fileList.Count == 0) yield break;
            int conWidth = context.ConWidth;
            fileList.Sort(StringComparer.InvariantCulture);
            int max = Math.Min(fileList.Select(e => Path.GetFileName(e.AsSpan()).Length).Max() + 1,
                Math.Max(10, conWidth));
            int pos = 0;
            foreach (string x in fileList)
            {
                ReadOnlySpan<char> fn = Path.GetFileName(x.AsSpan());
                if (pos + max >= conWidth)
                {
                    sb.Append('\n');
                    pos = 0;
                }

                sb.Append(fn);
                sb.Append(' ', max - fn.Length);
                pos += max;
            }

            sb.Append('\n');
            user.WriteEventSafe(Output(sb.ToString()));

            user.FlushSafeAsync();
        }
    }
}

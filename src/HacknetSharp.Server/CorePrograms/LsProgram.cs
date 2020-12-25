using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:ls", "ls", "list directory contents",
        "list contents of specified directory\nor current working directory",
        "[directory]", false)]
    public class LsProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            string path;
            try
            {
                path = Argv.Length > 1
                    ? Combine(Shell.WorkingDirectory, Argv[1])
                    : Shell.WorkingDirectory;
            }
            catch
            {
                yield break;
            }

            if (!System.TryGetFile(path, Login, out var result, out var closestStr, out var readable) &&
                path != "/")
                switch (result)
                {
                    case ReadAccessResult.NotReadable:
                        Write($"ls: {closestStr}: Permission denied\n");
                        yield break;
                    case ReadAccessResult.NoExist:
                        Write($"ls: {path}: No such file or directory\n");
                        yield break;
                    default:
                        yield break;
                }

            StringBuilder sb = new();

            List<string> fileList = readable == null || readable.Kind == FileModel.FileKind.Folder
                ? new List<string>(System.EnumerateDirectory(path).Select(f => f.Name))
                : new List<string> {readable.Name};
            if (fileList.Count == 0) yield break;
            int conWidth = Context.ConWidth;
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
            Write(sb.ToString());
        }
    }
}

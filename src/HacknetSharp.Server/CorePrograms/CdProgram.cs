using System.Collections.Generic;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:cd", "cd", "change working directory",
        "change current working directory to\nspecified directory or home directory\n",
        "[directory]", false)]
    public class CdProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Argv.Length == 1) yield break;
            string path;
            try
            {
                path = GetNormalized(Combine(Shell.WorkingDirectory, Argv[1]));
            }
            catch
            {
                yield break;
            }

            if (path == "/")
                Shell.WorkingDirectory = "/";
            else if (System.TryGetFile(path, Login, out var result, out var closestStr, out var closest))
                switch (closest.Kind)
                {
                    case FileModel.FileKind.TextFile:
                    case FileModel.FileKind.FileFile:
                    case FileModel.FileKind.ProgFile:
                        Write($"cd: {path}: Is a file\n").Flush();
                        yield break;
                    case FileModel.FileKind.Folder:
                        Shell.WorkingDirectory = path;
                        yield break;
                }
            else
                switch (result)
                {
                    case ReadAccessResult.NotReadable:
                        Write($"cd: {closestStr}: Permission denied\n").Flush();
                        yield break;
                    case ReadAccessResult.NoExist:
                        Write($"cd: {path}: No such file or directory\n").Flush();
                        yield break;
                }
        }
    }
}

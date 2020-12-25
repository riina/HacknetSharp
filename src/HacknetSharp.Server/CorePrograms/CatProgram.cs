using System.Collections.Generic;
using System.Linq;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:cat", "cat", "concatenate and print files",
        "print provided files sequentially\nin command-line order\n",
        "[files...]", false)]
    public class CatProgram : Program
    {
        private static readonly OutputEvent _newlineOutput = new() {Text = "\n"};

        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Argv.Length == 1)
            {
                Write("At least 1 operand is required by this command\n");
                yield break;
            }

            foreach (var file in Argv.Skip(1))
            {
                string path = GetNormalized(Combine(Shell.WorkingDirectory, file));
                if (path == "/")
                {
                    Write($"cat: {path}: Is a directory\n");
                    continue;
                }

                if (System.TryGetFile(path, Login, out var result, out var closestStr, out var closest))
                    switch (closest.Kind)
                    {
                        case FileModel.FileKind.TextFile:
                            Write(closest.Content ?? "").WriteEvent(_newlineOutput);
                            break;
                        case FileModel.FileKind.FileFile:
                            Write($"cat: {path}: Is a binary file\n");
                            break;
                        case FileModel.FileKind.ProgFile:
                            Write($"cat: {path}: Is a binary file\n");
                            break;
                        case FileModel.FileKind.Folder:
                            Write($"cat: {path}: Is a directory\n");
                            break;
                    }
                else
                    switch (result)
                    {
                        case ReadAccessResult.NotReadable:
                            Write($"cat: {closestStr}: Permission denied\n");
                            yield break;
                        case ReadAccessResult.NoExist:
                            Write($"cat: {path}: No such file or directory\n");
                            yield break;
                    }
            }
        }
    }
}

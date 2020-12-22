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
                Write(Output("At least 1 operand is required by this command\n")).Flush();
                yield break;
            }

            foreach (var file in Argv.Skip(1))
            {
                string path = GetNormalized(Combine(Shell.WorkingDirectory, file));
                if (path == "/")
                {
                    Write(Output($"cat: {path}: Is a directory\n"));
                    continue;
                }

                if (System.TryGetFile(path, Login, out var result, out var closestStr, out var closest))
                    switch (closest.Kind)
                    {
                        case FileModel.FileKind.TextFile:
                            Write(Output(closest.Content ?? "")).Write(_newlineOutput);
                            break;
                        case FileModel.FileKind.FileFile:
                            Write(Output($"cat: {path}: Is a binary file\n"));
                            break;
                        case FileModel.FileKind.ProgFile:
                            Write(Output($"cat: {path}: Is a binary file\n"));
                            break;
                        case FileModel.FileKind.Folder:
                            Write(Output($"cat: {path}: Is a directory\n"));
                            break;
                    }
                else
                    switch (result)
                    {
                        case ReadAccessResult.NotReadable:
                            Write(Output($"cat: {closestStr}: Permission denied\n")).Flush();
                            yield break;
                        case ReadAccessResult.NoExist:
                            Write(Output($"cat: {path}: No such file or directory\n")).Flush();
                            yield break;
                    }
            }

            Flush();
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:rm", "rm", "remove files or directories",
        "remove specified files or directories",
        "<files>...", false)]
    public class RmProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Argv.Length < 2)
            {
                Write("At least 1 operand is required by this command:\n\t<file>...\n");
                yield break;
            }

            try
            {
                var spawn = World.Spawn;
                string workDir = Shell.WorkingDirectory;
                foreach (var input in Argv[1..])
                {
                    string inputFmt = GetNormalized(Combine(workDir, input));
                    if (System.TryGetFile(inputFmt, Login, out var result, out var closestStr, out var closest))
                    {
                        try
                        {
                            string fp = closest.FullPath;
                            if (closest.Kind == FileModel.FileKind.Folder && System.Files.Any(f => f.Path == fp))
                            {
                                Write($"{inputFmt}: Directory not empty\n");
                                yield break;
                            }

                            spawn.RemoveFile(closest, Login);
                        }
                        catch (IOException e)
                        {
                            Write($"{e.Message}\n");
                            yield break;
                        }
                    }
                    else
                        switch (result)
                        {
                            case ReadAccessResult.NotReadable:
                                Write($"{closestStr}: Permission denied\n");
                                yield break;
                            case ReadAccessResult.NoExist:
                                Write($"{inputFmt}: No such file or directory\n");
                                yield break;
                        }
                }
            }
            catch (Exception e)
            {
                Write($"{e.Message}\n");
            }
        }
    }
}

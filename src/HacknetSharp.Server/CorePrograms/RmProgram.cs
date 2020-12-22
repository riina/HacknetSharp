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
                Write(Output("At least 1 operand is required by this command:\n\t<file>...\n")).Flush();
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
                                Write(Output($"{inputFmt}: Directory not empty\n")).Flush();
                                yield break;
                            }

                            spawn.RemoveFile(closest, Login);
                        }
                        catch (IOException e)
                        {
                            Write(Output($"{e.Message}\n")).Flush();
                            yield break;
                        }
                    }
                    else
                        switch (result)
                        {
                            case ReadAccessResult.NotReadable:
                                Write(Output($"{closestStr}: Permission denied\n")).Flush();
                                yield break;
                            case ReadAccessResult.NoExist:
                                Write(Output($"{inputFmt}: No such file or directory\n")).Flush();
                                yield break;
                        }
                }
            }
            catch (Exception e)
            {
                Write(Output($"{e.Message}\n")).Flush();
            }
        }
    }
}

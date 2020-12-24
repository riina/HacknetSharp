using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:cp", "cp", "copy files and directories",
        "copy source files to specified destination",
        "<source>... <dest>", false)]
    public class CpProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Argv.Length < 3)
            {
                Write("At least 2 operands are required by this command:\n\t<source>... <dest>\n").Flush();
                yield break;
            }

            try
            {
                string target;
                string workDir = Shell.WorkingDirectory;
                try
                {
                    target = GetNormalized(Combine(workDir, Argv[^1]));
                }
                catch
                {
                    yield break;
                }

                var spawn = World.Spawn;
                foreach (var input in Argv[1..^1])
                {
                    string inputFmt = GetNormalized(Combine(workDir, input));
                    if (System.TryGetFile(inputFmt, Login, out var result, out var closestStr, out var closest))
                    {
                        // Prevent copying common root to subdirectory
                        if (GetPathInCommon(inputFmt, target) == inputFmt)
                        {
                            Write($"{inputFmt}: Cannot copy to {target}\n").Flush();
                            yield break;
                        }

                        try
                        {
                            var targetExisting =
                                System.Files.FirstOrDefault(f => f.Hidden == false && f.FullPath == target);
                            string lclTarget;
                            string lclName;
                            if (target == "/" || Argv.Length != 3 || targetExisting != null
                                && targetExisting.Kind == FileModel.FileKind.Folder)
                            {
                                lclTarget = target;
                                lclName = closest.Name;
                            }
                            else
                            {
                                lclTarget = GetDirectoryName(target) ?? "/";
                                lclName = GetFileName(target);
                            }

                            spawn.CopyFile(closest, System, Login, lclName, lclTarget);
                        }
                        catch (IOException e)
                        {
                            Write($"{e.Message}\n").Flush();
                            yield break;
                        }
                    }
                    else
                        switch (result)
                        {
                            case ReadAccessResult.NotReadable:
                                Write($"{closestStr}: Permission denied\n").Flush();
                                yield break;
                            case ReadAccessResult.NoExist:
                                Write($"{inputFmt}: No such file or directory\n").Flush();
                                yield break;
                        }
                }
            }
            catch (Exception e)
            {
                Write($"{e.Message}\n").Flush();
            }
        }
    }
}

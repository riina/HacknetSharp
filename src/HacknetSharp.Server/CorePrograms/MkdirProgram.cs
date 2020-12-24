using System;
using System.Collections.Generic;
using System.IO;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:mkdir", "mkdir", "make directories",
        "create directories if they don't exist",
        "<dir>...", false)]
    public class MkdirProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Argv.Length < 2)
            {
                Write("At least 1 operand is required by this command:\n\t<dir>...\n").Flush();
                yield break;
            }

            try
            {
                var spawn = World.Spawn;
                string workDir = Shell.WorkingDirectory;
                foreach (var input in Argv[1..])
                {
                    string inputFmt = GetNormalized(Combine(workDir, input));
                    if (inputFmt == "/") continue;
                    System.TryGetFile(inputFmt, Login, out var result, out var closestStr, out _);
                    {
                        switch (result)
                        {
                            case ReadAccessResult.Readable:
                                Write($"{inputFmt}: Path exists\n").Flush();
                                yield break;
                            case ReadAccessResult.NotReadable:
                                Write($"{closestStr}: Permission denied\n").Flush();
                                yield break;
                            case ReadAccessResult.NoExist:
                                try
                                {
                                    var (path, name) = GetDirectoryAndName(inputFmt);
                                    spawn.Folder(System, Login, name, path);
                                }
                                catch (IOException e)
                                {
                                    Write($"{e.Message}\n").Flush();
                                    yield break;
                                }

                                break;
                        }
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

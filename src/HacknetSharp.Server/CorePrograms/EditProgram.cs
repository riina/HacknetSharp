using System.Collections.Generic;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:edit", "edit", "edit file",
        "opens the specified file for editing\nor for viewing if not writable",
        "[file]", false)]
    public class EditProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Argv.Length != 2)
            {
                Write("1 operand is required by this command\n");
                yield break;
            }

            string file = Argv[1];
            string path = GetNormalized(Combine(Shell.WorkingDirectory, file));
            if (path == "/")
            {
                Write($"{path}: Is a directory\n");
                yield break;
            }

            if (System.TryGetFile(path, Login, out var result, out var closestStr, out var closest))
                switch (closest.Kind)
                {
                    case FileModel.FileKind.TextFile:
                        bool editable = closest.CanWrite(Context.Login);
                        var edit = Edit(User, !editable, closest.Content ?? "");
                        yield return edit;
                        if (editable)
                        {
                            closest.Content = edit.Edit!.Content;
                            World.Database.Update(closest);
                        }

                        break;
                    case FileModel.FileKind.FileFile:
                        Write($"edit: {path}: Is a binary file\n");
                        break;
                    case FileModel.FileKind.ProgFile:
                        Write($"edit: {path}: Is a binary file\n");
                        break;
                    case FileModel.FileKind.Folder:
                        Write($"edit: {path}: Is a directory\n");
                        break;
                }
            else
                switch (result)
                {
                    case ReadAccessResult.Readable:
                        break;
                    case ReadAccessResult.NotReadable:
                        Write($"{closestStr}: Permission denied\n");
                        yield break;
                    case ReadAccessResult.NoExist:
                        var (directory, name) = GetDirectoryAndName(path);
                        if (directory != "/")
                        {
                            if (!System.TryGetFile(directory, Login, out var result2,
                                out string closest2Str, out var closest2))
                            {
                                switch (result2)
                                {
                                    case ReadAccessResult.NotReadable:
                                        Write($"{closest2Str}: Permission denied\n");
                                        break;
                                    case ReadAccessResult.NoExist:
                                        Write($"{directory}: No such file or directory\n");
                                        break;
                                }

                                yield break;
                            }

                            if (!closest2.CanWrite(Login))
                            {
                                Write($"{path}: Permission denied\n");
                                yield break;
                            }
                        }

                        var edit = Edit(false, "");
                        yield return edit;
                        var edited = edit.Edit!;
                        if (edited.Write)
                        {
                            // check file again. if it exists, just... fucking drop it for now
                            if (!System.TryGetFile(path, Login, out var result3, out _, out _) ||
                                result3 != ReadAccessResult.NotReadable)
                            {
                                closest = World.Spawn.TextFile(System, Login, name, directory, edit.Edit!.Content);
                                if (closest.Content!.Length > ServerConstants.MaxFileLength)
                                    closest.Content = closest.Content[..ServerConstants.MaxFileLength];
                            }
                        }

                        yield break;
                }
        }
    }
}

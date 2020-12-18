using System.Collections.Generic;
using System.Linq;
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
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var system = context.System;
            var argv = context.Argv;
            if (argv.Length != 2)
            {
                user.WriteEventSafe(Output("1 operand is required by this command\n"));
                user.FlushSafeAsync();
                yield break;
            }

            foreach (var file in argv.Skip(1))
            {
                string path = GetNormalized(Combine(context.Shell.WorkingDirectory, file));
                if (path == "/")
                {
                    user.WriteEventSafe(Output($"{path}: Is a directory\n"));
                    continue;
                }

                if (system.TryGetFile(path, context.Login, out var result, out var closestStr, out var closest))
                    switch (closest.Kind)
                    {
                        case FileModel.FileKind.TextFile:
                            bool editable = closest.CanWrite(context.Login);
                            var edit = Edit(user, !editable, closest.Content ?? "");
                            yield return edit;
                            if (editable)
                            {
                                closest.Content = edit.Edit!.Content;
                                context.World.Database.Update(closest);
                            }

                            break;
                        case FileModel.FileKind.FileFile:
                            user.WriteEventSafe(Output($"cat: {path}: Is a binary file\n"));
                            break;
                        case FileModel.FileKind.ProgFile:
                            user.WriteEventSafe(Output($"cat: {path}: Is a binary file\n"));
                            break;
                        case FileModel.FileKind.Folder:
                            user.WriteEventSafe(Output($"cat: {path}: Is a directory\n"));
                            break;
                    }
                else
                    switch (result)
                    {
                        case ReadAccessResult.Readable:
                            break;
                        case ReadAccessResult.NotReadable:
                            user.WriteEventSafe(Output($"{closestStr}: Permission denied\n"));
                            user.FlushSafeAsync();
                            yield break;
                        case ReadAccessResult.NoExist:
                            var (directory, name) = GetDirectoryAndName(path);
                            if (!system.TryGetFile(directory, context.Login, out var result2,
                                out string closest2Str, out var closest2))
                            {
                                switch (result2)
                                {
                                    case ReadAccessResult.NotReadable:
                                        user.WriteEventSafe(Output($"{closest2Str}: Permission denied\n"));
                                        user.FlushSafeAsync();
                                        break;
                                    case ReadAccessResult.NoExist:
                                        user.WriteEventSafe(Output($"{directory}: No such file or directory\n"));
                                        user.FlushSafeAsync();
                                        break;
                                }

                                yield break;
                            }
                            else if (!closest2.CanWrite(context.Login))
                            {
                                user.WriteEventSafe(Output($"{path}: Permission denied\n"));
                                user.FlushSafeAsync();
                                yield break;
                            }

                            closest = context.World.Spawn.TextFile(context.System, context.Login, name, directory, "");
                            bool editable = closest.CanWrite(context.Login);
                            var edit = Edit(user, !editable, closest.Content ?? "");
                            yield return edit;
                            var edited = edit.Edit!;
                            if (editable && edited.Write)
                            {
                                closest.Content = edit.Edit!.Content;
                                if (closest.Content.Length > ServerConstants.MaxFileLength)
                                    closest.Content = closest.Content[..ServerConstants.MaxFileLength];
                                context.World.Database.Update(closest);
                            }

                            yield break;
                    }
            }

            user.FlushSafeAsync();
        }
    }
}

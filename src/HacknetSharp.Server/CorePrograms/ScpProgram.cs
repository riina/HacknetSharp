using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:scp", "scp", "copy remote file or directory",
        "copy source file/directory from remote machine\nto specified destination",
        "<username>@<server>:<source> <dest>", false)]
    public class ScpProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var argv = context.Argv;

            if (!ServerUtil.TryParseScpConString(argv.Length == 2 ? "" : argv[1], out string? name, out string? host,
                out string? path,
                out string? error, context.Shell.TryGetVariable("USER", out string? shellUser) ? shellUser : null,
                context.Shell.TryGetVariable("TARGET", out string? shellTarget) ? shellTarget : null))
            {
                if (argv.Length < 3)
                {
                    user.WriteEventSafe(
                        Output(
                            "scp: 2 operands are required by this command:\n\t<username>@<server>:<source> <dest>\n"));
                    user.FlushSafeAsync();
                }
                else
                {
                    user.WriteEventSafe(Output($"scp: {error}\n"));
                    user.FlushSafeAsync();
                }

                yield break;
            }

            if (!IPAddressRange.TryParse(host, false, out var range) ||
                !range.TryGetIPv4HostAndSubnetMask(out uint hostUint, out _))
            {
                user.WriteEventSafe(Output($"scp: Invalid host {host}\n"));
                user.FlushSafeAsync();
                yield break;
            }

            string password;
            if (context.Shell.TryGetVariable("PASS", out string? shellPass))
                password = shellPass;
            else
            {
                user.WriteEventSafe(Output("Password:"));
                user.FlushSafeAsync();
                var input = Input(user, true);
                yield return input;
                password = input.Input!.Input;
            }

            var rSystem = context.World.Model.Systems.FirstOrDefault(s => s.Address == hostUint);
            if (rSystem == null)
            {
                user.WriteEventSafe(Output("scp: No route to host\n"));
                user.FlushSafeAsync();
                yield break;
            }

            var rLogin = rSystem.Logins.FirstOrDefault(l => l.User == name);
            if (rLogin == null || !ServerUtil.ValidatePassword(password, rLogin.Hash, rLogin.Salt))
            {
                user.WriteEventSafe(Output("scp: Invalid credentials\n"));
                user.FlushSafeAsync();
                yield break;
            }

            try
            {
                string target;
                string workDir = context.Shell.WorkingDirectory;
                try
                {
                    target = GetNormalized(Combine(workDir, argv[2]));
                }
                catch
                {
                    yield break;
                }

                var spawn = context.World.Spawn;
                var login = context.Login;
                var system = context.System;

                string inputFmt = GetNormalized(path);
                if (rSystem.TryGetWithAccess(inputFmt, rLogin, out var result, out var closestStr, out var closest))
                {
                    try
                    {
                        var targetExisting =
                            rSystem.Files.FirstOrDefault(f => f.Hidden == false && f.FullPath == target);
                        string lclTarget;
                        string lclName;
                        if (target == "/" || argv.Length != 3 || targetExisting != null
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

                        //Console.WriteLine($"Duplicating to [{lclTarget}] [{lclName}]");

                        spawn.Duplicate(system, login, lclName, lclTarget, closest);
                    }
                    catch (IOException e)
                    {
                        user.WriteEventSafe(Output($"{e.Message}\n"));
                        user.FlushSafeAsync();
                    }
                }
                else
                    switch (result)
                    {
                        case ReadAccessResult.NotReadable:
                            user.WriteEventSafe(Output($"{closestStr}: Permission denied\n"));
                            user.FlushSafeAsync();
                            yield break;
                        case ReadAccessResult.NoExist:
                            user.WriteEventSafe(Output($"{inputFmt}: No such file or directory\n"));
                            user.FlushSafeAsync();
                            yield break;
                    }
            }
            catch (Exception e)
            {
                user.WriteEventSafe(Output($"{e.Message}\n"));
                user.FlushSafeAsync();
            }
        }
    }
}

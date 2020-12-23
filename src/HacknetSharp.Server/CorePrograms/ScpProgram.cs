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
        public override IEnumerator<YieldToken?> Run()
        {
            if (!ServerUtil.TryParseScpConString(Argv.Length < 2 ? "" : Argv[1], out string? name, out string? host,
                out string? path, out string? error,
                Shell.TryGetVariable("NAME", out string? shellUser) ? shellUser : null,
                Shell.TryGetVariable("HOST", out string? shellTarget) ? shellTarget : null))
            {
                if (Argv.Length < 3)
                {
                    Write(
                            Output(
                                "scp: 2 operands are required by this command:\n\t<username>@<server>:<source> <dest>\n"))
                        .Flush();
                }
                else
                {
                    Write(Output($"scp: {error}\n")).Flush();
                }

                yield break;
            }

            if (!IPAddressRange.TryParse(host, false, out var range) ||
                !range.TryGetIPv4HostAndSubnetMask(out uint hostUint, out _))
            {
                Write(Output($"scp: Invalid host {host}\n")).Flush();
                yield break;
            }

            string? password = null;
            if (Shell.TryGetVariable("PASS", out string? shellPass))
                password = shellPass;
            else
            {
                try
                {
                    if (LoginManager.GetLogins(Login, hostUint).TryGetValue(name, out var storedLogin))
                        password = storedLogin.Pass;
                }
                catch (IOException e)
                {
                    Write(Output($"{e.Message}\n")).Flush();
                    yield break;
                }

                if (password == null)
                {
                    Write(Output("Password:"));
                    var input = Input(true);
                    yield return input;
                    password = input.Input!.Input;
                }
            }

            if (!World.Model.AddressedSystems.TryGetValue(hostUint, out var rSystem))
            {
                Write(Output("scp: No route to host\n")).Flush();
                yield break;
            }

            var rLogin = rSystem.Logins.FirstOrDefault(l => l.User == name);
            if (rLogin == null || !ServerUtil.ValidatePassword(password, rLogin.Hash, rLogin.Salt))
            {
                Write(Output("scp: Invalid credentials\n")).Flush();
                yield break;
            }

            try
            {
                string target;
                string workDir = Shell.WorkingDirectory;
                try
                {
                    target = GetNormalized(Combine(workDir, Argv[2]));
                }
                catch
                {
                    yield break;
                }

                var spawn = World.Spawn;

                string inputFmt = GetNormalized(path);
                if (rSystem.TryGetFile(inputFmt, rLogin, out var result, out var closestStr, out var closest))
                {
                    // Prevent copying common root to subdirectory
                    if (System == rSystem && GetPathInCommon(inputFmt, target) == inputFmt)
                    {
                        Write(Output($"{inputFmt}: Cannot copy to {target}\n")).Flush();
                        yield break;
                    }

                    try
                    {
                        var targetExisting =
                            rSystem.Files.FirstOrDefault(f => f.Hidden == false && f.FullPath == target);
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
                        Write(Output($"{e.Message}\n")).Flush();
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
            catch (Exception e)
            {
                Write(Output($"{e.Message}\n")).Flush();
            }
        }
    }
}

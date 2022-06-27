using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:scp", "scp", "copy remote file or directory",
        "copy source file/directory from remote machine\nto specified destination\n" +
        "If server isn't specified, connected server is used.",
        "<username>@<server>:<source> <dest>", false)]
    public class ScpProgram : Program
    {
        private const string AutoLoginHost = "$AUTO_HOST";

        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Argv.Length != 2 && Argv.Length != 3)
            {
                Write(
                    "scp: 1 or 2 operands are required by this command:\n\t<username>@<server>:<source> [<dest>]\n");
                yield break;
            }

            if (!ServerUtil.TryParseScpConString(Argv[1], out string? name, out string? host, out string? path,
                    out string? error, Shell.TryGetVariable("NAME", out string? shellUser) ? shellUser : null,
                    AutoLoginHost))
            {
                Write($"scp: {error}\n");
                yield break;
            }

            uint hostUint;
            if (host == AutoLoginHost)
            {
                if (Shell.Target != null)
                    hostUint = Shell.Target.Address;
                else
                {
                    Write("No server specified, and not currently connected to a server\n");
                    yield break;
                }
            }
            else if (!IPAddressRange.TryParse(host, false, out var range) ||
                     !range.TryGetIPv4HostAndSubnetMask(out hostUint, out _))
            {
                Write($"Invalid host {host}\n");
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
                    Write($"{e.Message}\n");
                    yield break;
                }

                if (password == null)
                {
                    Write("Password:");
                    var input = Input(true);
                    yield return input;
                    password = input.Input!.Input;
                }
            }

            if (!World.Model.AddressedSystems.TryGetValue(hostUint, out var rSystem))
            {
                Write("scp: No route to host\n");
                yield break;
            }

            var rLogin = rSystem.Logins.FirstOrDefault(l => l.User == name);
            if (rLogin == null || !ServerUtil.ValidatePassword(password, rLogin.Password))
            {
                Write("scp: Invalid credentials\n");
                yield break;
            }

            try
            {
                string targetFmt;
                string workDir = Shell.WorkingDirectory;
                try
                {
                    targetFmt = GetNormalized(Combine(workDir, Argv.Length == 3 ? Argv[2] : "."));
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
                    if (System == rSystem && GetPathInCommon(inputFmt, targetFmt) == inputFmt)
                    {
                        Write($"{inputFmt}: Cannot copy to {targetFmt}\n");
                        yield break;
                    }

                    try
                    {
                        string lclTarget;
                        string lclName;
                        if (targetFmt == "/" ||
                            System.TryGetFile(targetFmt, Login, out _, out _, out var targetExisting)
                            && targetExisting.Kind == FileModel.FileKind.Folder)
                        {
                            lclTarget = targetFmt;
                            lclName = closest.Name;
                        }
                        else
                        {
                            lclTarget = GetDirectoryName(targetFmt) ?? "/";
                            lclName = GetFileName(targetFmt);
                        }

                        spawn.CopyFile(closest, System, Login, lclName, lclTarget);
                    }
                    catch (IOException e)
                    {
                        Write($"{e.Message}\n");
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
            catch (Exception e)
            {
                Write($"{e.Message}\n");
            }
        }
    }
}

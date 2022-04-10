using System.Collections.Generic;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:ssh", "ssh", "connect to remote machine",
        "opens an authenticated connection to a\nremote machine and opens a shell",
        "username@server", true)]
    public class SshProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (!ServerUtil.TryParseConString(Argv.Length == 1 ? "" : Argv[1], 22, out string? name, out string? host,
                    out _, out string? error))
            {
                Write($"{error}\n");
                yield break;
            }

            if (!IPAddressRange.TryParse(host, false, out var range) ||
                !range.TryGetIPv4HostAndSubnetMask(out uint hostUint, out _))
            {
                Write($"Invalid host {host}\n");
                yield break;
            }

            string password;
            if (Shell.TryGetVariable("PASS", out string? shellPass))
                password = shellPass;
            else
            {
                Write("Password:");
                var input = Input(true);
                yield return input;
                password = input.Input!.Input;
            }

            Connect(hostUint, name, password);
        }
    }
}

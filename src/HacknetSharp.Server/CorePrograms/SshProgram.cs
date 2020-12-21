using System.Collections.Generic;
using System.Linq;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:ssh", "ssh", "connect to remote machine",
        "opens an authenticated connection to a\nremote machine and opens a shell",
        "username@server", false)]
    public class SshProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            string[] argv = context.Argv;

            if (!ServerUtil.TryParseConString(argv.Length == 1 ? "" : argv[1], 22, out string? name, out string? host,
                out _, out string? error))
            {
                user.WriteEventSafe(Output($"{error}\n"));
                user.FlushSafeAsync();
                yield break;
            }

            if (!IPAddressRange.TryParse(host, false, out var range) ||
                !range.TryGetIPv4HostAndSubnetMask(out uint hostUint, out _))
            {
                user.WriteEventSafe(Output($"Invalid host {host}\n"));
                user.FlushSafeAsync();
                yield break;
            }

            string password;
            if (context.Shell.TryGetVariable("PASS", out string? shellPass))
                password = shellPass;
            else
            {
                user.WriteEventSafe(Output("Password:"));
                var input = Input(user, true);
                yield return input;
                password = input.Input!.Input;
            }

            Connect(context, hostUint, name, password);
        }
    }
}

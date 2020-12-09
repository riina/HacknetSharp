using System.Collections.Generic;
using System.Linq;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:ssh", "ssh", "connect to remote machine",
        "opens an authenticated connection\n" +
        "to a remote machine and opens a shell:",
        "ssh username@server", false)]
    public class SshProgram : Program
    {
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var argv = context.Argv;
            if (argv.Length == 1)
            {
                user.WriteEventSafe(Output("ssh: Needs connection target\n"));
                user.FlushSafeAsync();
                yield break;
            }

            if (!ServerUtil.TryParseConString(argv[1], 22, out string? name, out string? host, out ushort port,
                out string? error))
            {
                user.WriteEventSafe(Output($"ssh: {error}\n"));
                user.FlushSafeAsync();
                yield break;
            }

            if (!IPAddressRange.TryParse(host, false, out var range) ||
                !range.TryGetIPv4HostAndSubnetMask(out uint hostUint, out _))
            {
                user.WriteEventSafe(Output($"ssh: Invalid host {host}\n"));
                user.FlushSafeAsync();
                yield break;
            }

            user.WriteEventSafe(Output("Password:"));
            user.FlushSafeAsync();
            var input = Input(user, true);
            yield return input;
            user.WriteEventSafe(Output("Connecting...\n"));
            var system = context.World.Model.Systems.FirstOrDefault(s => s.Address == hostUint);
            if (system == null)
            {
                user.WriteEventSafe(Output("ssh: No route to host\n"));
                user.FlushSafeAsync();
                yield break;
            }

            var login = system.Logins.FirstOrDefault(l => l.User == name);
            if (login == null || !ServerUtil.ValidatePassword(input.Input!.Input, login.Hash, login.Salt))
            {
                user.WriteEventSafe(Output("ssh: Invalid credentials\n"));
                user.FlushSafeAsync();
                yield break;
            }

            context.World.StartShell(user, context.Person, system, login, "");
            if (system.ConnectCommandLine != null)
            {
                var chainLine = Arguments.SplitCommandLine(system.ConnectCommandLine);
                if (chainLine.Length != 0 && !string.IsNullOrWhiteSpace(chainLine[0]))
                    context.ChainLine = chainLine;
            }

            user.FlushSafeAsync();
        }
    }
}

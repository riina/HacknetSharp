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
            var argv = context.Argv;

            if (!ServerUtil.TryParseConString(argv.Length == 1 ? "" : argv[1], 22, out string? name, out string? host,
                out _, out string? error,
                context.Shell.TryGetVariable("USER", out string? shellUser) ? shellUser : null,
                context.Shell.TryGetVariable("TARGET", out string? shellTarget) ? shellTarget : null))
            {
                if (argv.Length == 1)
                {
                    user.WriteEventSafe(Output("ssh: Needs connection target\n"));
                    user.FlushSafeAsync();
                }
                else
                {
                    user.WriteEventSafe(Output($"ssh: {error}\n"));
                    user.FlushSafeAsync();
                }

                yield break;
            }

            if (!IPAddressRange.TryParse(host, false, out var range) ||
                !range.TryGetIPv4HostAndSubnetMask(out uint hostUint, out _))
            {
                user.WriteEventSafe(Output($"ssh: Invalid host {host}\n"));
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

            user.WriteEventSafe(Output("Connecting...\n"));
            if (!context.World.TryGetSystem(hostUint, out var system))
            {
                user.WriteEventSafe(Output("ssh: No route to host\n"));
                user.FlushSafeAsync();
                yield break;
            }

            var login = system.Logins.FirstOrDefault(l => l.User == name);
            if (login == null || !ServerUtil.ValidatePassword(password, login.Hash, login.Salt))
            {
                user.WriteEventSafe(Output("ssh: Invalid credentials\n"));
                user.FlushSafeAsync();
                yield break;
            }

            context.World.StartShell(user, context.Person, system, login, ServerConstants.ShellName);
            if (context.System.KnownSystems.All(p => p.To != system))
                context.World.Spawn.Connection(context.System, system, false);
            if (system.ConnectCommandLine != null)
            {
                var chainLine = ServerUtil.SplitCommandLine(system.ConnectCommandLine);
                if (chainLine.Length != 0 && !string.IsNullOrWhiteSpace(chainLine[0]))
                    context.ChainLine = chainLine;
            }

            user.FlushSafeAsync();
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using HacknetSharp.Server;

namespace hss.Core.CorePrograms
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
                yield break;
            }

            if (!ServerUtil.TryParseConString(argv[1], 22, out string? name, out string? host, out ushort port,
                out string? error))
            {
                user.WriteEventSafe(Output($"ssh: {error}\n"));
                yield break;
            }

            if (!IPAddressRange.TryParse(host, false, out var range) ||
                !range.TryGetIPv4HostAndSubnetMask(out uint hostUint, out _))
            {
                user.WriteEventSafe(Output($"ssh: Invalid host {host}\n"));
                yield break;
            }

            user.WriteEventSafe(Output("Password:"));
            var input = Input(user, true);
            yield return input;
            user.WriteEventSafe(Output("Connecting...\n"));
            var system = context.World.Model.Systems.FirstOrDefault(s => s.Address == hostUint);
            if (system == null)
            {
                user.WriteEventSafe(Output("ssh: No route to host\n"));
                yield break;
            }

            var login = system.Logins.FirstOrDefault(l => l.User == name);
            if (login == null || !ServerUtil.ValidatePassword(input.Input!.Input, login.Hash, login.Salt))
            {
                user.WriteEventSafe(Output("ssh: Invalid credentials\n"));
                yield break;
            }

            context.System = new HacknetSharp.Server.System(context.World, system);
            context.Person.LoginChain.Add(login);
            user.FlushSafeAsync();
        }
    }
}

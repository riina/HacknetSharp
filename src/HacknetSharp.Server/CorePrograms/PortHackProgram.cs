using System.Collections.Generic;
using System.Linq;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:porthack", "PortHack", "bruteforce login",
        "Obtains an administrator login on\nthe target system\n\n" +
        "target system can be assumed from environment\nvariable \"TARGET\".\n" +
        "This program sets TARGET, NAME, and PASS\nenvironment variables.",
        "[target]", false)]
    public class PortHackProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            string[] argv = context.Argv;
            var shell = context.Shell;

            if (!TryGetVariable(context, argv.Length != 1 ? argv[1] : null, "TARGET", out string? addr))
            {
                user.WriteEventSafe(Output("No address provided\n"));
                user.FlushSafeAsync();
                yield break;
            }

            if (!TryGetSystem(context.World.Model, addr, out var system, out string? systemConnectError))
            {
                user.WriteEventSafe(Output($"{systemConnectError}\n"));
                user.FlushSafeAsync();
                yield break;
            }

            if (system.FirewallIterations > 0 &&
                (!shell.FirewallStates.TryGetValue(system.Address, out var firewallState) ||
                 !firewallState.solved))
            {
                user.WriteEventSafe(Output(
                    "Failed: Firewall active.\n"));
                user.FlushSafeAsync();
                yield break;
            }

            shell.OpenVulnerabilities.TryGetValue(system.Address, out var openVulns);
            int sum = openVulns?.Aggregate(0, (c, v) => c + v.Exploits) ?? 0;
            if (sum < system.RequiredExploits)
            {
                user.WriteEventSafe(Output(
                    $"Failed: insufficient exploits established.\nCurrent: {sum}\nRequired: {system.RequiredExploits}\n"));
                user.FlushSafeAsync();
                yield break;
            }

            user.WriteEventSafe(Output("«««« RUNNING PORTHACK »»»»\n"));
            SignalUnbindProcess(context, null);

            yield return Delay(6.0f);

            string un = ServerUtil.GenerateUser();
            string pw = ServerUtil.GeneratePassword();
            var (hash, salt) = ServerUtil.HashPassword(pw);
            context.World.Spawn.Login(system, un, hash, salt, true);
            shell.SetVariable("TARGET", addr);
            shell.SetVariable("NAME", un);
            shell.SetVariable("PASS", pw);
            user.WriteEventSafe(Output($"\n«««« OPERATION COMPLETE »»»»\n$NAME: {un}\n$PASS: {pw}\n"));
            user.FlushSafeAsync();
        }
    }
}

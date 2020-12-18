using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:probe", "probe", "probe remote system",
        "probe a system's ports to determine\nwhat vulnerabilities exist on the system\n\n" +
        "target system can be assumed from environment\nvariable \"TARGET\"",
        "[system]", false)]
    public class ProbeProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            string[] argv = context.Argv;

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

            user.WriteEventSafe(Output($"Probing {Util.UintToAddress(system.Address)}...\n"));
            user.FlushSafeAsync();

            yield return Delay(1.0f);

            var sb = new StringBuilder();

            if (system.FirewallIterations > 0)
            {
                if (context.Shell.FirewallStates.TryGetValue(system.Address, out var firewallState) &&
                    firewallState.solved)
                    sb.Append("\nFirewall: BYPASSED\n");
                else
                    sb.Append("\nFirewall: ACTIVE\n");
            }

            sb.Append("\nVulnerabilities:\n");
            context.Shell.OpenVulnerabilities.TryGetValue(system.Address, out var vulns);
            foreach (var vuln in system.Vulnerabilities)
            {
                string openStr = vulns?.Contains(vuln) ?? false ? "OPEN" : "CLOSED";
                sb.Append(
                    $"{vuln.EntryPoint,-8}: {vuln.Protocol} ({openStr}, {vuln.Exploits} exploit(s), {vuln.Cve ?? "unknown CVEs"})\n");
            }

            sb.Append($"\nRequired exploits: {system.RequiredExploits}\n");
            sb.Append($"Current exploits: {vulns?.Aggregate(0, (n, v) => n + v.Exploits) ?? 0}\n");
            user.WriteEventSafe(Output(sb.ToString()));
            user.FlushSafeAsync();
        }
    }
}

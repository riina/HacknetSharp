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
        public override IEnumerator<YieldToken?> Run()
        {
            if (!TryGetVariable(Argv.Length != 1 ? Argv[1] : null, "TARGET", out string? addr))
            {
                Write(Output("No address provided\n")).Flush();
                yield break;
            }

            if (!TryGetSystem(addr, out var system, out string? systemConnectError))
            {
                Write(Output($"{systemConnectError}\n")).Flush();
                yield break;
            }

            Write(Output($"Probing {Util.UintToAddress(system.Address)}...\n")).Flush();

            yield return Delay(1.0f);

            var sb = new StringBuilder();

            if (system.FirewallIterations > 0)
            {
                if (Shell.FirewallStates.TryGetValue(system.Address, out var firewallState) &&
                    firewallState.solved)
                    sb.Append("\nFirewall: BYPASSED\n");
                else
                    sb.Append("\nFirewall: ACTIVE\n");
            }

            sb.Append("\nVulnerabilities:\n");
            Shell.OpenVulnerabilities.TryGetValue(system.Address, out var vulns);
            foreach (var vuln in system.Vulnerabilities)
            {
                string openStr = vulns?.Contains(vuln) ?? false ? "OPEN" : "CLOSED";
                sb.Append(
                    $"{vuln.EntryPoint,-8}: {vuln.Protocol} ({openStr}, {vuln.Exploits} exploit(s), {vuln.Cve ?? "unknown CVEs"})\n");
            }

            sb.Append($"\nRequired exploits: {system.RequiredExploits}\n");
            sb.Append($"Current exploits: {vulns?.Aggregate(0, (n, v) => n + v.Exploits) ?? 0}\n");
            Write(Output(sb.ToString())).Flush();
        }
    }
}

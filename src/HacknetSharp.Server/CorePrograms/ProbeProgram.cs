using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:probe", "probe", "probe remote system",
        "probe a system's ports to determine\nwhat vulnerabilities exist on the system\n\n" +
        "target system can be assumed from environment\nvariable \"HOST\"",
        "[system]", false)]
    public class ProbeProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (!TryGetVariable(Argv.Length != 1 ? Argv[1] : null, "HOST", out string? addr))
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

            var crackState = Shell.GetCrackState(system);

            if (system.ProxyClocks > 0)
                sb.Append($"Proxy: {Math.Clamp(100.0 * crackState.ProxyClocks / system.ProxyClocks, 0, 100):F1}% bypassed\n");

            if (system.FirewallIterations > 0)
                sb.Append(crackState.FirewallSolved ? "\nFirewall: BYPASSED\n" : "\nFirewall: ACTIVE\n");

            if (crackState.OpenVulnerabilities.Count != 0)
            {
                sb.Append("\nVulnerabilities:\n");
                foreach (var vuln in system.Vulnerabilities)
                {
                    string openStr = crackState.OpenVulnerabilities.Contains(vuln) ? "OPEN" : "CLOSED";
                    sb.Append(
                        $"{vuln.EntryPoint,-8}: {vuln.Protocol} ({openStr}, {vuln.Exploits} exploit(s), {vuln.Cve ?? "unknown CVEs"})\n");
                }
            }

            sb.Append($"\nRequired exploits: {system.RequiredExploits}\n");
            sb.Append($"Current exploits: {crackState.OpenVulnerabilities.Aggregate(0, (n, v) => n + v.Exploits)}\n");
            Write(Output(sb.ToString())).Flush();
        }
    }
}

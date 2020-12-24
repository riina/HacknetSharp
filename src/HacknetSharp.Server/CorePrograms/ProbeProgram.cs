using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:probe", "probe", "probe remote system",
        "probe a system's ports to determine\nwhat vulnerabilities exist on the system\n" +
        "If server isn't specified, connected server is used.",
        "[server]", false)]
    public class ProbeProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            SystemModel? system;
            if (Argv.Length >= 2)
            {
                if (!TryGetSystem(Argv[1], out system, out string? systemConnectError))
                {
                    Write($"{systemConnectError}\n").Flush();
                    yield break;
                }
            }
            else
            {
                if (Shell.Target != null)
                    system = Shell.Target;
                else
                {
                    Write("No server specified, and not currently connected to a server\n").Flush();
                    yield break;
                }
            }

            Write($"Probing {Util.UintToAddress(system.Address)}...\n").Flush();

            yield return Delay(1.0f);

            // If server happened to go down in between, escape.
            if (Shell.Target == null || !TryGetSystem(system.Address, out _, out _))
            {
                Write("Error: connection to server lost\n").Flush();
                yield break;
            }

            var sb = new StringBuilder();

            var crackState = Shell.GetCrackState(system);

            if (system.ProxyClocks > 0)
                sb.Append(
                    $"Proxy: {Math.Clamp(100.0 * crackState.ProxyClocks / system.ProxyClocks, 0, 100):F1}% bypassed\n");

            if (system.FirewallIterations > 0)
                sb.Append(crackState.FirewallSolved ? "\nFirewall: BYPASSED\n" : "\nFirewall: ACTIVE\n");

            if (system.Vulnerabilities.Count != 0)
            {
                sb.Append("\nVulnerabilities:\n");
                foreach (var vuln in system.Vulnerabilities)
                {
                    string openStr = crackState.OpenVulnerabilities.ContainsKey(vuln) ? "OPEN" : "CLOSED";
                    sb.Append(
                        $"{vuln.EntryPoint,-8}: {vuln.Protocol} ({openStr}, {vuln.Exploits} exploit(s), {vuln.Cve ?? "unknown CVEs"})\n");
                }
            }

            sb.Append($"\nRequired exploits: {system.RequiredExploits}\n");
            sb.Append(
                $"Current exploits: {crackState.OpenVulnerabilities.Aggregate(0, (n, v) => n + v.Key.Exploits)}\n");
            Write(sb.ToString()).Flush();
        }
    }
}

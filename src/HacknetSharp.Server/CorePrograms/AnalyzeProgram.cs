using System;
using System.Collections.Generic;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:analyze", "analyze", "analyze firewall",
        "Analyzes firewall to progressively get solution\n\n" +
        "target system can be assumed from environment\nvariable \"TARGET\"",
        "[target]", false)]
    public class AnalyzeProgram : Program
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

            if (system.FirewallIterations <= 0)
            {
                Write(Output("Firewall not active.\n")).Flush();
                yield break;
            }

            if (!Shell.FirewallStates.TryGetValue(system.Address, out var firewallState))
            {
                firewallState.solution = system.FixedFirewall ?? ServerUtil.GeneratePassword(system.FirewallIterations);
                firewallState.iterations = 0;
                firewallState.solved = false;
            }

            firewallState.iterations = Math.Min(firewallState.solution.Length, firewallState.iterations + 1);

            string[] analysisLines = ServerUtil.GenerateFirewallAnalysis(firewallState.solution,
                firewallState.iterations, system.FirewallLength);

            Write(Output($"Pass {firewallState.iterations}...\n")).Flush();
            double delay = system.FirewallDelay * firewallState.iterations;
            foreach (var analysisLine in analysisLines)
            {
                yield return Delay(delay);
                Write(Output($"{analysisLine}\n")).Flush();
            }

            Shell.FirewallStates[system.Address] = firewallState;
        }
    }
}

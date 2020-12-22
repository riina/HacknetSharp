using System;
using System.Collections.Generic;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:analyze", "analyze", "analyze firewall",
        "Analyzes firewall to progressively get solution\n\n" +
        "target system can be assumed from environment\nvariable \"HOST\"",
        "[target]", false)]
    public class AnalyzeProgram : Program
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

            if (system.FirewallIterations <= 0)
            {
                Write(Output("Firewall not active.\n")).Flush();
                yield break;
            }

            var crackState = Shell.GetCrackState(system);

            crackState.FirewallIterations =
                Math.Min(crackState.FirewallSolution.Length, crackState.FirewallIterations + 1);

            string[] analysisLines = ServerUtil.GenerateFirewallAnalysis(crackState.FirewallSolution,
                crackState.FirewallIterations, system.FirewallLength);

            Write(Output($"Pass {crackState.FirewallIterations}...\n")).Flush();
            double delay = system.FirewallDelay * crackState.FirewallIterations;
            foreach (var analysisLine in analysisLines)
            {
                yield return Delay(delay);
                Write(Output($"{analysisLine}\n")).Flush();
            }
        }
    }
}

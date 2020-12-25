using System;
using System.Collections.Generic;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:analyze", "analyze", "analyze firewall",
        "Analyzes firewall to progressively get solution.",
        "", false)]
    public class AnalyzeProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            SystemModel? system;
            if (Shell.Target != null)
                system = Shell.Target;
            else
            {
                Write("Not currently connected to a server\n");
                yield break;
            }

            if (system.FirewallIterations <= 0)
            {
                Write("Firewall not active.\n");
                yield break;
            }

            var crackState = Shell.GetCrackState(system);

            crackState.FirewallIterations =
                Math.Min(crackState.FirewallSolution.Length, crackState.FirewallIterations + 1);

            string[] analysisLines = ServerUtil.GenerateFirewallAnalysis(crackState.FirewallSolution,
                crackState.FirewallIterations, system.FirewallLength);

            Write($"Pass {crackState.FirewallIterations}...\n");
            double delay = system.FirewallDelay * crackState.FirewallIterations;
            foreach (var analysisLine in analysisLines)
            {
                yield return Delay(delay);
                // If server happened to go down in between, escape.
                if (Shell.Target == null || !TryGetSystem(system.Address, out _, out _))
                {
                    Write("Error: connection to server lost\n");
                    yield break;
                }

                Write($"{analysisLine}\n");
            }
        }
    }
}

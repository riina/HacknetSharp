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

            if (system.FirewallIterations <= 0)
            {
                user.WriteEventSafe(Output("Firewall not active.\n"));
                user.FlushSafeAsync();
                yield break;
            }

            if (!shell.FirewallStates.TryGetValue(system.Address, out var firewallState))
            {
                firewallState.solution = system.FixedFirewall ?? ServerUtil.GeneratePassword(system.FirewallIterations);
                firewallState.iterations = 0;
                firewallState.solved = false;
            }

            firewallState.iterations = Math.Min(firewallState.solution.Length, firewallState.iterations + 1);

            string[] analysisLines = ServerUtil.GenerateFirewallAnalysis(firewallState.solution,
                firewallState.iterations, system.FirewallLength);

            user.WriteEventSafe(Output($"Pass {firewallState.iterations}...\n"));
            double delay = system.FirewallDelay * firewallState.iterations;
            foreach (var analysisLine in analysisLines)
            {
                yield return Delay(delay);
                user.WriteEventSafe(Output($"{analysisLine}\n"));
                user.FlushSafeAsync();
            }

            shell.FirewallStates[system.Address] = firewallState;
        }
    }
}

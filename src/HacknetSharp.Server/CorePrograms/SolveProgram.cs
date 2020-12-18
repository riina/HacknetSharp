using System.Collections.Generic;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:solve", "solve", "solve firewall challenge",
        "attempt to bypass firewall with specified solution\n\n" +
        "target system can be assumed from environment\nvariable \"TARGET\"",
        "[target] <solution>", false)]
    public class SolveProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            string[] argv = context.Argv;
            var shell = context.Shell;

            if (argv.Length != 2 && argv.Length != 3)
            {
                user.WriteEventSafe(Output("1 or 2 operands are required by this command\n"));
                user.FlushSafeAsync();
                yield break;
            }

            if (!TryGetVariable(context, argv.Length == 3 ? argv[1] : null, "TARGET", out string? addr))
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
                user.WriteEventSafe(Output("Firewall not active on target system.\n"));
                user.FlushSafeAsync();
                yield break;
            }

            user.WriteEventSafe(Output("Solving firewall...\n"));
            user.FlushSafeAsync();

            yield return Delay(6);

            // TODO timed programs should maintain handle to remote system in case of reported shutdown...

            bool fail;
            string solution = argv.Length == 3 ? argv[2] : argv[1];
            if (system.FixedFirewall != null)
                fail = system.FixedFirewall != solution;
            else
                fail = !shell.FirewallStates.TryGetValue(system.Address, out var state) || state.solution != solution;

            if (fail)
            {
                user.WriteEventSafe(Output("Incorrect solution. Bypass failed.\n"));
                user.FlushSafeAsync();
            }
            else
            {
                if (shell.FirewallStates.TryGetValue(system.Address, out var state))
                    shell.FirewallStates[system.Address] = (state.solution, state.iterations, true);
                else
                    shell.FirewallStates[system.Address] = (solution, 0, true);
                user.WriteEventSafe(Output("Firewall bypassed.\n"));
                user.FlushSafeAsync();
            }
        }
    }
}

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
        public override IEnumerator<YieldToken?> Run()
        {
            if (Argv.Length != 2 && Argv.Length != 3)
            {
                Write(Output("1 or 2 operands are required by this command\n")).Flush();
                yield break;
            }

            if (!TryGetVariable(Argv.Length == 3 ? Argv[1] : null, "TARGET", out string? addr))
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
                Write(Output("Firewall not active on target system.\n")).Flush();
                yield break;
            }

            Write(Output("Solving firewall...\n")).Flush();

            yield return Delay(6);

            // TODO timed programs should maintain handle to remote system in case of reported shutdown...

            bool fail;
            string solution = Argv.Length == 3 ? Argv[2] : Argv[1];
            if (system.FixedFirewall != null)
                fail = system.FixedFirewall != solution;
            else
                fail = !Shell.FirewallStates.TryGetValue(system.Address, out var state) || state.solution != solution;

            if (fail)
            {
                Write(Output("Incorrect solution. Bypass failed.\n")).Flush();
            }
            else
            {
                if (Shell.FirewallStates.TryGetValue(system.Address, out var state))
                    Shell.FirewallStates[system.Address] = (state.solution, state.iterations, true);
                else
                    Shell.FirewallStates[system.Address] = (solution, 0, true);
                Write(Output("Firewall bypassed.\n")).Flush();
            }
        }
    }
}

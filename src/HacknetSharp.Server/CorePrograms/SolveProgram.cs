using System.Collections.Generic;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:solve", "solve", "solve firewall challenge",
        "attempt to bypass firewall with specified solution",
        "<solution>", false)]
    public class SolveProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Argv.Length != 2)
            {
                Write("1 operand are required by this command\n");
                yield break;
            }

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
                Write("Firewall not active on target system.\n");
                yield break;
            }

            Write("Solving firewall...\n");

            yield return Delay(6);

            // TODO timed programs should maintain handle to remote system in case of reported shutdown...

            // If server happened to go down in between, escape.
            if (Shell.Target == null || !TryGetSystem(system.Address, out _, out _))
            {
                Write("Error: connection to server lost\n");
                yield break;
            }

            string solution = Argv[1];
            var crackState = Shell.GetCrackState(system);
            if (crackState.FirewallSolution != solution)
                Write("Incorrect solution. Bypass failed.\n");
            else
            {
                crackState.FirewallSolved = true;
                Write("Firewall bypassed.\n");
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:hack", "{HARG:1}", "{HARG:1} exploit",
        "Attempts to execute {HARG:1} exploit\non specified port or entrypoint\non server.",
        "<port/entrypoint>", false)]
    public class HackProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            // Need harg[1] = protocol to hack harg[2] hack time
            if (HArgv.Length < 3 || !float.TryParse(HArgv[2], out float hackTime))
            {
                Write("This program is corrupt and cannot be executed.\n");
                yield break;
            }

            if (Argv.Length != 2)
            {
                Write("1 operand is required by this command\n");
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

            string entryPoint = Argv[1];

            var vuln = system.Vulnerabilities.FirstOrDefault(v =>
                string.Equals(v.EntryPoint, entryPoint, StringComparison.InvariantCultureIgnoreCase));

            if (vuln == null)
            {
                Write("Entrypoint is closed\n");
                yield break;
            }

            string protocol = HArgv[1];

            if (!string.Equals(vuln.Protocol, protocol, StringComparison.InvariantCultureIgnoreCase))
            {
                Write("Unexpected protocol on entrypoint\n");
                yield break;
            }


            Write($"«««« RUNNING {Argv[0]} »»»»\n");
            SignalUnbindProcess();

            yield return Delay(hackTime);

            // If server happened to go down in between, escape.
            if (Shell.Target == null || !TryGetSystem(system.Address, out _, out _))
            {
                Write("Error: connection to server lost\n");
                yield break;
            }

            Shell.GetCrackState(system).OpenVulnerability(vuln);

            Write($"\n«««« {Argv[0]} COMPLETE »»»»\n");
        }
    }
}

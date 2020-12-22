using System;
using System.Collections.Generic;
using System.Linq;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:hack", "{HARG:1}", "{HARG:1} exploit",
        "Attempts to execute {HARG:1} exploit\non specified port or entrypoint\non target system\n\n" +
        "target system can be assumed from environment\nvariable \"TARGET\"",
        "[target] <port/entrypoint>", false)]
    public class HackProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            // Need harg[1] = protocol to hack harg[2] hack time
            if (HArgv.Length < 3 || !float.TryParse(HArgv[2], out float hackTime))
            {
                Write(Output("This program is corrupt and cannot be executed.\n")).Flush();
                yield break;
            }

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

            string entryPoint = Argv.Length == 2 ? Argv[1] : Argv[2];

            var vuln = system.Vulnerabilities.FirstOrDefault(v =>
                string.Equals(v.EntryPoint, entryPoint, StringComparison.InvariantCultureIgnoreCase));

            if (vuln == null)
            {
                Write(Output("Entrypoint is closed\n")).Flush();
                yield break;
            }

            string protocol = HArgv[1];

            if (!string.Equals(vuln.Protocol, protocol, StringComparison.InvariantCultureIgnoreCase))
            {
                Write(Output("Unexpected protocol on entrypoint\n")).Flush();
                yield break;
            }


            Write(Output($"«««« RUNNING {Argv[0]} »»»»\n"));
            SignalUnbindProcess(null);

            yield return Delay(hackTime);

            if (!Shell.OpenVulnerabilities.TryGetValue(system.Address, out var openVulns))
                openVulns = Shell.OpenVulnerabilities[system.Address] = new HashSet<VulnerabilityModel>();

            openVulns.Add(vuln);

            Write(Output($"\n«««« {Argv[0]} COMPLETE »»»»\n")).Flush();
        }
    }
}

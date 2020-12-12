using System.Collections.Generic;
using System.Linq;
using System.Text;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:probe", "probe", "probe remote system",
        "probe a system's ports to determine\nwhat vulnerabilities exist on the system\n\n" +
        "target system can be assumed from environment\nvariable \"TARGET\"",
        "[system]", false)]
    public class ProbeProgram : Program
    {
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var argv = context.Argv;
            SystemModel? system;
            if (argv.Length != 1)
            {
                if (!IPAddressRange.TryParse(argv[1], false, out var ip) ||
                    !ip.TryGetIPv4HostAndSubnetMask(out uint host, out _))
                {
                    user.WriteEventSafe(Output("Invalid address format.\n"));
                    user.FlushSafeAsync();
                    yield break;
                }

                system = context.World.Model.Systems.FirstOrDefault(s => s.Address == host);
                if (system == null)
                {
                    user.WriteEventSafe(Output("No route to host\n"));
                    user.FlushSafeAsync();
                    yield break;
                }
            }
            else
            {
                if (!context.Shell.Variables.ContainsKey("TARGET"))
                {
                    user.WriteEventSafe(Output("No host address provided.\n"));
                    user.FlushSafeAsync();
                    yield break;
                }

                if (!context.Shell.TryGetTarget(out system))
                {
                    user.WriteEventSafe(Output("No route to host\n"));
                    user.FlushSafeAsync();
                    yield break;
                }
            }

            user.WriteEventSafe(Output($"Probing {ServerUtil.UintToAddress(system.Address)}...\n"));
            user.FlushSafeAsync();

            yield return Delay(1.0f);

            var sb = new StringBuilder();
            sb.Append("\nVulnerabilities:\n");
            foreach (var vuln in system.Vulnerabilities)
                sb.Append(
                    $"{vuln.EntryPoint,-8}: {vuln.Protocol} ({vuln.Exploits} exploit(s), {vuln.Cve ?? "unknown CVEs"})\n");
            sb.Append($"\nRequired exploits: {system.RequiredExploits}\n");
            user.WriteEventSafe(Output(sb.ToString()));
            user.FlushSafeAsync();
        }
    }
}

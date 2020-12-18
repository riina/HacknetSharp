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
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            string[] hargv = context.HArgv;

            // Need harg[1] = protocol to hack harg[2] hack time
            if (hargv.Length < 3 || !float.TryParse(hargv[2], out float hackTime))
            {
                user.WriteEventSafe(Output("This program is corrupt and cannot be executed.\n"));
                user.FlushSafeAsync();
                yield break;
            }

            string[] argv = context.Argv;
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

            string entryPoint = argv.Length == 2 ? argv[1] : argv[2];

            var vuln = system.Vulnerabilities.FirstOrDefault(v =>
                string.Equals(v.EntryPoint, entryPoint, StringComparison.InvariantCultureIgnoreCase));

            if (vuln == null)
            {
                user.WriteEventSafe(Output("Entrypoint is closed\n"));
                user.FlushSafeAsync();
                yield break;
            }

            string protocol = hargv[1];

            if (!string.Equals(vuln.Protocol, protocol, StringComparison.InvariantCultureIgnoreCase))
            {
                user.WriteEventSafe(Output("Unexpected protocol on entrypoint\n"));
                user.FlushSafeAsync();
                yield break;
            }


            user.WriteEventSafe(Output($"«««« RUNNING {argv[0]} »»»»\n"));
            SignalUnbindProcess(context, null);

            yield return Delay(hackTime);

            if (!context.Shell.OpenVulnerabilities.TryGetValue(system.Address, out var openVulns))
                openVulns = context.Shell.OpenVulnerabilities[system.Address] = new HashSet<VulnerabilityModel>();

            openVulns.Add(vuln);

            user.WriteEventSafe(Output($"\n«««« {argv[0]} COMPLETE »»»»\n"));
            user.FlushSafeAsync();
        }
    }
}

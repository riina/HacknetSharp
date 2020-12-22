using System.Collections.Generic;
using System.Linq;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:porthack", "PortHack", "bruteforce login",
        "Obtains an administrator login on\nthe target system\n\n" +
        "target system can be assumed from environment\nvariable \"TARGET\".\n" +
        "This program sets TARGET, NAME, and PASS\nenvironment variables.",
        "[target]", false)]
    public class PortHackProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (!TryGetVariable(Argv.Length != 1 ? Argv[1] : null, "TARGET", out string? addr))
            {
                Write(Output("No address provided\n")).Flush();
                yield break;
            }

            if (!TryGetSystem(addr, out var system, out string? systemConnectError))
            {
                Write(Output($"{systemConnectError}\n")).Flush();
                yield break;
            }

            if (system.FirewallIterations > 0 &&
                (!Shell.FirewallStates.TryGetValue(system.Address, out var firewallState) ||
                 !firewallState.solved))
            {
                Write(Output("Failed: Firewall active.\n")).Flush();
                yield break;
            }

            Shell.OpenVulnerabilities.TryGetValue(system.Address, out var openVulns);
            int sum = openVulns?.Aggregate(0, (c, v) => c + v.Exploits) ?? 0;
            if (sum < system.RequiredExploits)
            {
                Write(Output(
                        $"Failed: insufficient exploits established.\nCurrent: {sum}\nRequired: {system.RequiredExploits}\n"))
                    .Flush();
                yield break;
            }

            Write(Output("«««« RUNNING PORTHACK »»»»\n"));
            SignalUnbindProcess(null);

            yield return Delay(6.0f);

            string un = ServerUtil.GenerateUser();
            string pw = ServerUtil.GeneratePassword();
            var (hash, salt) = ServerUtil.HashPassword(pw);
            World.Spawn.Login(system, un, hash, salt, true);
            Shell.SetVariable("TARGET", addr);
            Shell.SetVariable("NAME", un);
            Shell.SetVariable("PASS", pw);
            Write(Output($"\n«««« OPERATION COMPLETE »»»»\n$NAME: {un}\n$PASS: {pw}\n")).Flush();
        }
    }
}

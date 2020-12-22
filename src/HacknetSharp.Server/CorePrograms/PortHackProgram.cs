using System.Collections.Generic;
using System.Linq;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:porthack", "PortHack", "bruteforce login",
        "Obtains an administrator login on\nthe target system\n\n" +
        "target system can be assumed from environment\nvariable \"HOST\".\n" +
        "This program sets HOST, NAME, and PASS\nenvironment variables.",
        "[target]", false)]
    public class PortHackProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (!TryGetVariable(Argv.Length != 1 ? Argv[1] : null, "HOST", out string? addr))
            {
                Write(Output("No address provided\n")).Flush();
                yield break;
            }

            if (!TryGetSystem(addr, out var system, out string? systemConnectError))
            {
                Write(Output($"{systemConnectError}\n")).Flush();
                yield break;
            }

            var crackState = Shell.GetCrackState(system);

            if (system.FirewallIterations > 0 && !crackState.FirewallSolved)
            {
                Write(Output("Failed: Firewall active.\n")).Flush();
                yield break;
            }

            if (crackState.ProxyClocks < system.ProxyClocks)
            {
                Write(Output("Failed: Proxy active.\n")).Flush();
                yield break;
            }

            int sum = crackState.OpenVulnerabilities.Aggregate(0, (c, v) => c + v.Exploits);
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
            Shell.SetVariable("HOST", addr);
            Shell.SetVariable("NAME", un);
            Shell.SetVariable("PASS", pw);
            Write(Output($"\n«««« OPERATION COMPLETE »»»»\n$NAME: {un}\n$PASS: {pw}\n")).Flush();
        }
    }
}

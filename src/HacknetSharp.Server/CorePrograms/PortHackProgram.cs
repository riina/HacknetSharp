using System.Collections.Generic;
using System.IO;
using System.Linq;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:porthack", "PortHack", "bruteforce login",
        "Obtains an administrator login on\nthe target system\n\n" +
        "This program sets the NAME and PASS environment\n" +
        "variables and saves the login to your local login store.",
        "", false)]
    public class PortHackProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            SystemModel? system;
            if (Shell.Target != null)
                system = Shell.Target;
            else
            {
                Write("Not currently connected to a server\n");
                yield break;
            }

            var crackState = Shell.GetCrackState(system);

            if (system.FirewallIterations > 0 && !crackState.FirewallSolved)
            {
                Write("Failed: Firewall active.\n");
                yield break;
            }

            if (crackState.ProxyClocks < system.ProxyClocks)
            {
                Write("Failed: Proxy active.\n");
                yield break;
            }

            int sum = crackState.OpenVulnerabilities.Aggregate(0, (c, v) => c + v.Key.Exploits);
            if (sum < system.RequiredExploits)
            {
                Write(
                        $"Failed: insufficient exploits established.\nCurrent: {sum}\nRequired: {system.RequiredExploits}\n");
                yield break;
            }

            Write("«««« RUNNING PORTHACK »»»»\n");
            SignalUnbindProcess();

            yield return Delay(6.0f);

            // If server happened to go down in between, escape.
            if (Shell.Target == null || !TryGetSystem(system.Address, out _, out _))
            {
                Write("Error: connection to server lost\n");
                yield break;
            }

            string un = ServerUtil.GenerateUser();
            string pw = ServerUtil.GeneratePassword();
            var (hash, salt) = ServerUtil.HashPassword(pw);
            World.Spawn.Login(system, un, hash, salt, true);
            Shell.SetVariable("NAME", un);
            Shell.SetVariable("PASS", pw);
            Write($"\n«««« OPERATION COMPLETE »»»»\n$NAME: {un}\n$PASS: {pw}\n");
            try
            {
                LoginManager.AddLogin(World, Login, system.Address, un, pw);
            }
            catch (IOException e)
            {
                Write($"{e.Message}\n");
            }
        }
    }
}

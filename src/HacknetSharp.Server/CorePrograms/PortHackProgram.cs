using System.Collections.Generic;
using System.Linq;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:porthack", "PortHack", "bruteforce login",
        "Obtains an administrator login on\nthe target system\n\n" +
        "target system can be assumed from environment\nvariable \"TARGET\"",
        "[target] <port/entrypoint>", false)]
    public class PortHackProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var argv = context.Argv;

            if (!TryGetSystemOrOutput(context, argv.Length != 1 ? argv[1] : null, out var system))
                yield break;

            context.Shell.OpenVulnerabilities.TryGetValue(system.Address, out var openVulns);
            int sum = openVulns?.Aggregate(0, (c, v) => c + v.Exploits) ?? 0;
            if (sum < system.RequiredExploits)
            {
                user.WriteEventSafe(Output(
                    $"Insufficient exploits established.\nCurrent: {sum}\nRequired: {system.RequiredExploits}\n"));
                user.FlushSafeAsync();
                yield break;
            }

            user.WriteEventSafe(Output("«««« RUNNING PORTHACK »»»»\n"));
            SignalUnbindProcess(context, null);

            yield return Delay(6.0f);

            string un = ServerUtil.GenerateUser();
            string pw = ServerUtil.GeneratePassword();
            var pwHashSalt = ServerUtil.HashPassword(pw);
            context.World.Spawn.Login(system, un, pwHashSalt.hash, pwHashSalt.salt, true);
            var env = context.Shell.Variables;
            env["USER"] = un;
            env["PASS"] = pw;
            user.WriteEventSafe(Output($"\n«««« OPERATION COMPLETE »»»»\n$USER: {un}\n$PASS: {pw}\n"));
            user.FlushSafeAsync();
        }
    }
}

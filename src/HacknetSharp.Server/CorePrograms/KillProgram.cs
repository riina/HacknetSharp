using System.Collections.Generic;
using System.Linq;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:kill", "kill", "terminate a process",
        "sends a termination signal to a process",
        "<pid>...", false)]
    public class KillProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            string[] argv = context.Argv;
            var login = context.Login;
            var system = context.System;
            var world = context.World;
            foreach (var p in argv.Skip(1))
            {
                if (!ushort.TryParse(p, out ushort pid))
                {
                    user.WriteEventSafe(Output($"kill: {p}: arguments must be process ids"));
                    user.FlushSafeAsync();
                }

                if (system.Processes.TryGetValue(pid, out var proc) && login.Admin && proc.Context.Login == login)
                {
                    world.CompleteRecurse(proc, Process.CompletionKind.KillLocal);
                }
                else
                {
                    user.WriteEventSafe(Output($"kill: ({pid}) - No such process"));
                    user.FlushSafeAsync();
                }
            }
        }
    }
}

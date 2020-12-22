using System.Collections.Generic;
using System.Linq;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:kill", "kill", "terminate a process",
        "sends a termination signal to a process",
        "<pid>...", true)]
    public class KillProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            foreach (var p in Argv.Skip(1))
            {
                if (!ushort.TryParse(p, out ushort pid))
                {
                    Write(Output($"kill: {p}: arguments must be process ids")).Flush();
                }

                if (System.Processes.TryGetValue(pid, out var proc) && Login.Admin &&
                    proc.ProcessContext.Login == Login)
                {
                    World.CompleteRecurse(proc, Process.CompletionKind.KillLocal);
                }
                else
                {
                    Write(Output($"kill: ({pid}) - No such process")).Flush();
                }
            }

            yield break;
        }
    }
}

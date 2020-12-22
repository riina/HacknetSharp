using System.Collections.Generic;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:trap", "trap", "trap proxy overloaders",
        "triggers forkbomb on proxy-overloading systems\nusing a shell process",
        "<shell process>", true)]
    public class TrapProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Argv.Length != 2)
            {
                Write(Output("1 operand is required by this command\n")).Flush();
                yield break;
            }

            string p = Argv[1];
            if (!ushort.TryParse(p, out ushort pid))
                Write(Output($"trap: {p}: arguments must be process ids\n")).Flush();
            else if (!System.Processes.TryGetValue(pid, out var pr) || pr is not ProgramProcess proc ||
                     proc.ProgramContext.Remote == null)
                Write(Output($"trap: ({pid}) - Invalid process\n")).Flush();
            else
            {
                Write(Output("SENDING TRAP\n")).Flush();
                System.Pulse?.Invoke(SystemModel.TrapSignal.Singleton);
            }
        }
    }
}

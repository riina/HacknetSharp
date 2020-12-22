using System.Collections.Generic;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:exit", "exit", "disconnect from machine",
        "closes current shell and disconnects from machine",
        "", true)]
    public class ExitProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            World.CompleteRecurse(Shell, Process.CompletionKind.Normal);
            yield break;
        }
    }
}

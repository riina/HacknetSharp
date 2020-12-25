using System.Collections.Generic;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:dc", "dc", "disconnect from server",
        "disconnect from a remote server",
        "", true)]
    public class DcProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Shell.Target != null)
            {
                Shell.Target.TargetingShells.Remove(Shell);
                Shell.Target = null;
                Write("Disconnected.\n");
                yield break;
            }
        }
    }
}

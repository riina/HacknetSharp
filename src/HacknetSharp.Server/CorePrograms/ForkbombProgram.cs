using System.Collections.Generic;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:forkbomb", "forkbomb", "crash system",
        "consumes memory and crashes system",
        "", false)]
    public class ForkbombProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            Write("«««« RUNNING FORKBOMB »»»»\n");
            SignalUnbindProcess();
            int warningGate = 0;
            while (true)
            {
                Memory += (int)(ServerConstants.ForkbombRate * (World.Time - World.PreviousTime));
                int tf = (int)(100.0 * System.GetUsedMemory() / System.SystemMemory) / 25;
                if (tf > warningGate)
                {
                    Write($"\nMEMORY {tf * 25}% CONSUMED\n").Flush();
                    warningGate = tf;
                }

                yield return null;
            }
        }
    }
}

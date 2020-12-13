using System.Collections.Generic;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:exit", "exit", "disconnect from machine",
        "closes current shell and disconnects from machine",
        "", true)]
    public class ExitProgram : Program
    {
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            context.World.CompleteRecurse(context.Shell, Process.CompletionKind.Normal);
        }
    }
}

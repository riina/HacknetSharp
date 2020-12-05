using System.Collections.Generic;
using System.Linq;
using System.Text;
using HacknetSharp.Events.Server;
using HacknetSharp.Server;

namespace hss.Core.CorePrograms
{
    [ProgramInfo("core:ps", "ps", "process status",
        "display active processes on\n" +
        "this machine",
        "[-e]", false)]
    public class PsProgram : Program
    {
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var argv = context.Argv;
            bool all = false;
            foreach (var arg in argv)
            {
                switch (arg.ToLowerInvariant())
                {
                    case "-e":
                        all = true;
                        break;
                }
            }

            user.WriteEventSafe(new OutputEvent
            {
                Text = new StringBuilder()
                    .Append(" ACC    PID   PPID LINE\n").AppendJoin("\n",
                        context.System.Ps(context.Login, null, all ? (uint?)null : context.ParentPid)
                            .Select(proc =>
                            {
                                var c = proc.Context;
                                string owner = c is ProgramContext pc ? pc.Login.User : "SVCH";
                                return
                                    $"{owner,4} {c.Pid,6:D} {c.ParentPid,6:D} {new StringBuilder().AppendJoin(' ', c.Argv)}";
                            }))
                    .Append('\n')
                    .ToString()
            });
            user.FlushSafeAsync();
        }
    }
}

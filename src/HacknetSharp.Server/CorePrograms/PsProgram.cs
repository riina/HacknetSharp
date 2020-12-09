using System.Collections.Generic;
using System.Linq;
using System.Text;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:ps", "ps", "process status",
        "display active processes on this machine",
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
                if (arg.StartsWith('-'))
                    foreach (char c in arg[1..])
                        switch (char.ToLowerInvariant(c))
                        {
                            case 'e':
                                all = true;
                                break;
                        }
            }

            LoginModel? login = context.Login.Admin ? null : context.Login;

            user.WriteEventSafe(new OutputEvent
            {
                Text = new StringBuilder()
                    .Append("     ACC    PID   PPID LINE\n").AppendJoin("\n",
                        context.System.Ps(login, null, all ? (uint?)null : context.ParentPid)
                            .Select(proc =>
                            {
                                var c = proc.Context;
                                string owner = c is ProgramContext pc ? pc.Login.User : "SVCH";
                                if (owner.Length > 8)
                                    owner = owner.Substring(0, 8);
                                return
                                    $"{owner,8} {c.Pid,6:D} {c.ParentPid,6:D} {new StringBuilder().AppendJoin(' ', c.Argv)}";
                            }))
                    .Append('\n')
                    .ToString()
            });
            user.FlushSafeAsync();
        }
    }
}

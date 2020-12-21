using System.Collections.Generic;
using System.Linq;
using System.Text;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:ps", "ps", "process status",
        "display active processes on this machine",
        "[-e]", true)]
    public class PsProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var (flags, _, _) = IsolateArgvFlags(context.Argv);
            bool all = flags.Contains("e");
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

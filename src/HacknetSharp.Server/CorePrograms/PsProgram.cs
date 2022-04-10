using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:ps", "ps", "process status",
        "display active processes on this machine",
        "[-e]", true)]
    public class PsProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            var (flags, _, _) = IsolateArgvFlags(Argv);
            Write(new StringBuilder()
                .Append("     ACC    PID   PPID LINE\n").AppendJoin("\n",
                    System.Ps(Login.Admin ? null : Login, null, flags.Contains("e") ? null : ParentPid)
                        .Select(proc =>
                        {
                            var c = proc.ProcessContext;
                            string owner = c is ProgramContext pc ? pc.Login.User : "SVCH";
                            if (owner.Length > 8)
                                owner = owner.Substring(0, 8);
                            return
                                $"{owner,8} {c.Pid,6:D} {c.ParentPid,6:D} {new StringBuilder().AppendJoin(' ', c.Argv)}";
                        }))
                .Append('\n')
                .ToString()
            );
            yield break;
        }
    }
}

using System.Collections.Generic;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:probe", "probe", "probe remote system",
        "probe a system's ports to determine\nwhat vulnerabilities exist on the system\n\n" +
        "target system can be assumed from environment\nvariable \"TARGET\"",
        "[system]", false)]
    public class ProbeProgram : Program
    {
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var sb = new StringBuilder();
            foreach (var vuln in context.System.Vulnerabilities)
            {
                sb.Append($"{vuln.EntryPoint,-8}: ").Append(vuln.Protocol);
                if (vuln.Cve != null)
                    sb.Append(" (").Append(vuln.Cve).Append(')');
                sb.Append('\n');
            }

            user.WriteEventSafe(Output(sb.ToString()));
            user.FlushSafeAsync();
        }
    }
}

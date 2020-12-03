using System.Collections.Generic;
using System.Linq;
using System.Text;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:echo", "echo", "write arguments",
        "write specified arguments separated by\n" +
        "single spaces followed by newline",
        "[arguments]", false)]
    public class EchoProgram : Program
    {
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            user.WriteEventSafe(new OutputEvent
            {
                Text = new StringBuilder().AppendJoin(' ', context.Argv.Skip(1)).Append('\n').ToString()
            });
            user.FlushSafeAsync();
        }
    }
}

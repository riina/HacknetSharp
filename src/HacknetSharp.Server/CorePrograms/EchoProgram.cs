using System.Collections.Generic;
using System.Linq;
using System.Text;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:echo", "write arguments",
        "write specified arguments separated by\n" +
        "single spaces followed by newline" +
        "echo [arguments]")]
    public class EchoProgram : Program
    {
        public override IEnumerator<YieldToken?> Invoke(CommandContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(CommandContext context)
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

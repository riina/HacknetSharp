using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:" + ServerConstants.ShellName, ServerConstants.ShellName, "start shell",
        "starts system shell",
        "", true)]
    public class ShellProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var shell = context.World.StartShell(user, context.Person, context.System, context.Login,
                new StringBuilder().AppendJoin(' ', context.Argv.Skip(1).Prepend(ServerConstants.ShellName))
                    .ToString());
            // Ensure a prompt is printed for generated shell
            if (shell != null)
                user.WriteEventSafe(ServerUtil.CreatePromptEvent(shell));
            else
                user.WriteEventSafe(Output("Process creation failed: out of memory\n"));
        }
    }
}

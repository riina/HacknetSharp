using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:c", "c", "send chat message",
        "send chat message to active chat server.",
        "<message>", true)]
    public class CProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Shell.Chat == null || Shell.ChatRoom == null || Shell.ChatName == null)
            {
                Write(Output("No chat room is active in this shell.\n")).Flush();
                yield break;
            }

            string message = new StringBuilder().AppendJoin(' ', Argv.Skip(1)).ToString();
            Shell.Chat.SendMessage(Shell.ChatRoom, Login.Key, Shell.ChatName, message);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:echo", "echo", "write arguments",
        "write specified arguments separated by\nsingle spaces followed by newline",
        "[arguments]", true)]
    public class EchoProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            Write(Output(new StringBuilder().AppendJoin(' ', Argv.Skip(1)).Append('\n').ToString())).Flush();
            yield break;
        }
    }
}

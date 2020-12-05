using System.Collections.Generic;
using HacknetSharp.Server;

namespace hss.Core.CorePrograms
{
    [ProgramInfo("core:exit", "exit", "disconnect from machine",
        "closes current shell and\n" +
        "disconnects from machine",
        "", true)]
    public class ExitProgram : Program
    {
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var chain = context.Person.ShellChain;
            if (chain.Count != 0) chain[^1].Close = true;
        }
    }
}

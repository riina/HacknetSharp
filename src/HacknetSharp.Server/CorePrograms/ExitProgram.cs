using System.Collections.Generic;
using System.Linq;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:exit","exit", "disconnect from machine",
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
            var chain = context.Person.LoginChain;
            chain.RemoveAt(chain.Count - 1);
            if (chain.Count == 0)
                context.Disconnect = true;
            else
                context.System = new Common.System(context.World, chain[^1].System);
        }
    }
}

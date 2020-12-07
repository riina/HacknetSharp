using System.Collections.Generic;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:scan", "scan", "scan network",
        "scan network for known devices\n" +
        "and report status",
        "", false)]
    public class ScanProgram : Program
    {
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            if (context.Login.Person != context.System.Owner.Key)
            {
                user.WriteEventSafe(Output("Permission denied."));
                user.FlushSafeAsync();
                yield break;
            }

            var system = context.System;
            double curTime = context.World.Time;
            var sb = new StringBuilder();
            foreach (var s in system.KnownSystems)
            {
                sb.Append(s.To.BootTime > curTime ? "DOWN" : "UP  ")
                    .Append($"{CommonUtil.UintToAddress(s.To.Address),16}")
                    .Append(' ')
                    .Append(s.To.Name)
                    .Append('\n');
            }

            user.WriteEventSafe(Output(sb.ToString()));
            user.FlushSafeAsync();
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Text;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:scan", "scan", "scan network",
        "scan network for local systems and report status",
        "[system]...", false)]
    public class ScanProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            if (!context.Login.Admin)
            {
                user.WriteEventSafe(Output("Permission denied.\n"));
                user.FlushSafeAsync();
                yield break;
            }

            var argv = context.Argv;
            double curTime = context.World.Time;
            var sb = new StringBuilder();
            if (argv.Length != 1)
            {
                foreach (var arg in argv.Skip(1))
                {
                    if (!IPAddressRange.TryParse(arg, false, out var addr) ||
                        !addr.TryGetIPv4HostAndSubnetMask(out uint host, out _))
                    {
                        user.WriteEventSafe(Output("Invalid address format.\n"));
                        user.FlushSafeAsync();
                    }
                    else
                    {
                        var remote = context.World.Model.Systems.FirstOrDefault(s => s.Address == host);
                        if (remote == null)
                        {
                            user.WriteEventSafe(Output($"Invalid host {addr}\n"));
                            user.FlushSafeAsync();
                        }
                        else
                            PrintForSystem(remote, sb, curTime);
                    }
                }
            }
            else
            {
                var system = context.System;
                foreach (var s in system.KnownSystems.Where(s => s.Local))
                    PrintForSystem(s.To, sb, curTime);
            }

            user.WriteEventSafe(Output(sb.ToString()));
            user.FlushSafeAsync();
        }

        private static void PrintForSystem(SystemModel system, StringBuilder sb, double curTime)
        {
            sb.Append(system.BootTime > curTime ? "DOWN" : "UP  ")
                .Append($"{ServerUtil.UintToAddress(system.Address),16}")
                .Append(' ')
                .Append(system.Name)
                .Append('\n');
        }
    }
}

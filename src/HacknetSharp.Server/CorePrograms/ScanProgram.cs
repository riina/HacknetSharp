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
        public override IEnumerator<YieldToken?> Run()
        {
            if (!Login.Admin)
            {
                Write("Permission denied.\n");
                yield break;
            }

            string[] argv = Argv;
            double curTime = World.Time;
            var sb = new StringBuilder();
            if (argv.Length != 1)
            {
                foreach (var arg in argv.Skip(1))
                {
                    if (!IPAddressRange.TryParse(arg, false, out var addr) ||
                        !addr.TryGetIPv4HostAndSubnetMask(out uint host, out _))
                    {
                        Write("Invalid address format.\n");
                    }
                    else
                    {
                        if (!World.Model.AddressedSystems.TryGetValue(host, out var remote))
                        {
                            Write($"Invalid host {addr}\n");
                        }
                        else
                            PrintForSystem(remote, sb, curTime);
                    }
                }
            }
            else
            {
                foreach (var s in System.KnownSystems.Where(s => s.Local))
                    PrintForSystem(s.To, sb, curTime);
            }

            Write(sb.ToString());
        }

        private static void PrintForSystem(SystemModel system, StringBuilder sb, double curTime)
        {
            sb.Append(system.BootTime > curTime ? "DOWN" : "UP  ")
                .Append($"{Util.UintToAddress(system.Address),16}")
                .Append(' ')
                .Append(system.Name)
                .Append('\n');
        }
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server
{
    public abstract class Program : Executable<ProgramContext>
    {
        #region Utility methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OutputEvent Output(string message) => new OutputEvent {Text = message};

        public static bool TryGetSystemOrOutput(ProgramContext context, string? addr,
            [NotNullWhen(true)] out SystemModel? system)
        {
            var user = context.User;
            if (addr != null)
            {
                if (!IPAddressRange.TryParse(addr, false, out var ip) ||
                    !ip.TryGetIPv4HostAndSubnetMask(out uint host, out _))
                {
                    user.WriteEventSafe(Output("Invalid address format.\n"));
                    user.FlushSafeAsync();
                    system = null;
                    return false;
                }

                system = context.World.Model.Systems.FirstOrDefault(s => s.Address == host);
                if (system == null)
                {
                    user.WriteEventSafe(Output("No route to host\n"));
                    user.FlushSafeAsync();
                    return false;
                }

                return true;
            }

            if (!context.Shell.Variables.ContainsKey("TARGET"))
            {
                user.WriteEventSafe(Output("No host address provided.\n"));
                user.FlushSafeAsync();
                system = null;
                return false;
            }

            if (!context.Shell.TryGetTarget(out system))
            {
                user.WriteEventSafe(Output("No route to host\n"));
                user.FlushSafeAsync();
                return false;
            }

            return true;
        }

        #endregion
    }
}

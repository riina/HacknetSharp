using System.Runtime.CompilerServices;
using HacknetSharp.Events.Server;

namespace HacknetSharp.Server.Common
{
    public abstract class Service : Executable<ServiceContext>
    {
        #region Utility methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OutputEvent Output(string message) => new OutputEvent {Text = message};

        #endregion
    }
}

using System.Runtime.CompilerServices;
using HacknetSharp.Events.Server;

namespace HacknetSharp.Server
{
    public abstract class Program : Executable<ProgramContext>
    {
        #region Utility methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OutputEvent Output(string message) => new OutputEvent {Text = message};

        #endregion
    }
}

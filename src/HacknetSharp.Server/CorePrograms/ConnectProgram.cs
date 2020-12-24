using System.Collections.Generic;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:connect", "connect", "connect to server",
        "connect to a remote server for cracking",
        "<server>", true)]
    public class ConnectProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Argv.Length != 2)
            {
                Write(Output("1 operand is required by this command\n")).Flush();
                yield break;
            }

            SystemModel? system;
            if (!TryGetSystem(Argv[1], out system, out string? systemConnectError))
            {
                Write(Output($"{systemConnectError}\n")).Flush();
                yield break;
            }

            Shell.Target = system;
            system.TargetingShells.Add(Shell);

            Write(Output($"Connected to {system.Name}...\n")).Flush();
        }
    }
}

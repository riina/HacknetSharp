using System;
using System.Collections.Generic;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:overload", "overload", "overload proxy",
        "Attempts to overload target with specified shell process\n\n" +
        "target system can be assumed from environment\nvariable \"HOST\"",
        "[target] <shell process>", false)]
    public class OverloadProgram : Program
    {
        private SignalWaiter? _signalWaiter;

        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Argv.Length != 2 && Argv.Length != 3)
            {
                Write(Output("1 or 2 operands are required by this command\n")).Flush();
                yield break;
            }

            if (!TryGetVariable(Argv.Length == 3 ? Argv[1] : null, "HOST", out string? addr))
            {
                Write(Output("No address provided\n")).Flush();
                yield break;
            }

            if (!TryGetSystem(addr, out var system, out string? systemConnectError))
            {
                Write(Output($"{systemConnectError}\n")).Flush();
                yield break;
            }

            string p = Argv.Length == 2 ? Argv[1] : Argv[2];
            if (!ushort.TryParse(p, out ushort pid))
                Write(Output($"overload: {p}: arguments must be process ids\n")).Flush();
            else if (!System.Processes.TryGetValue(pid, out var pr) || pr is not ProgramProcess proc ||
                     proc.ProgramContext.Remote == null)
                Write(Output($"overload: ({pid}) - Invalid process\n")).Flush();
            else
            {
                var crackState = Shell.GetCrackState(system);
                var shellSystem = proc.ProgramContext.System;
                _signalWaiter = new SignalWaiter(system);
                Write(Output("«««« OVERLOADING PROXY »»»»\n"));
                SignalUnbindProcess(null);
                bool first = true;
                int warningGate = 0;
                while (crackState.ProxyClocks < system.ProxyClocks)
                {
                    if (_signalWaiter.Trapped)
                    {
                        if (first)
                        {
                            Write(Output("\n" +
                                         "»»»»   ERROR: OVERLOAD FAILED   ««««\n" +
                                         "»»     REMOTE TRAP TRIGGERED      ««\n" +
                                         "»»    MEMORY OVERFLOW DETECTED    ««\n" +
                                         "»»»» USER INTERVENTION REQUIRED ««««\n")).Flush();
                            first = false;
                        }

                        Memory += (int)(ServerConstants.ForkbombRate * (World.Time - World.PreviousTime));
                        int tf = (int)(100.0 * System.GetUsedMemory() / System.SystemMemory) / 25;
                        if (tf > warningGate)
                        {
                            Write(Output($"\nMEMORY {tf * 25}% CONSUMED\n")).Flush();
                            warningGate = tf;
                        }

                        yield return null;
                    }
                    else
                        crackState.ProxyClocks += shellSystem.ClockSpeed * (World.Time - World.PreviousTime);

                    yield return null;
                }

                Write(Output("«««« PROXY OVERLOAD COMPLETE »»»»\n")).Flush();
            }
        }

        /// <inheritdoc />
        public override bool OnShutdown()
        {
            _signalWaiter?.Dispose();
            return true;
        }

        private class SignalWaiter : IDisposable
        {
            public bool Trapped { get; private set; }
            private readonly SystemModel _systemModel;
            private bool _disposed;

            public SignalWaiter(SystemModel system)
            {
                _systemModel = system;
                system.Pulse += PulseHandler;
            }

            private void PulseHandler(object obj)
            {
                if (_disposed || Trapped) return;
                Trapped = obj is SystemModel.TrapSignal;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _systemModel.Pulse -= PulseHandler;
            }
        }
    }
}

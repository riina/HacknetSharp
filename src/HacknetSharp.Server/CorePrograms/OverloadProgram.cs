using System;
using System.Collections.Generic;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:overload", "overload", "overload proxy",
        "Attempts to overload server with specified shell process",
        "<shell process>", false)]
    public class OverloadProgram : Program
    {
        private SignalWaiter? _signalWaiter;

        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (Argv.Length != 2)
            {
                Write("1 operand is required by this command\n");
                yield break;
            }

            SystemModel? system;
            if (Shell.Target != null)
                system = Shell.Target;
            else
            {
                Write("Not currently connected to a server\n");
                yield break;
            }

            string p = Argv[1];
            if (!ushort.TryParse(p, out ushort pid))
                Write($"overload: {p}: arguments must be process ids\n");
            else if (!System.Processes.TryGetValue(pid, out var pr) || pr is not ProgramProcess proc ||
                     proc.ProgramContext.Remote == null)
                Write($"overload: ({pid}) - Invalid process\n");
            else
            {
                var crackState = Shell.GetCrackState(system);
                var shellSystem = proc.ProgramContext.System;
                _signalWaiter = new SignalWaiter(system);
                Write("«««« OVERLOADING PROXY »»»»\n");
                SignalUnbindProcess();
                bool first = true;
                int warningGate = 0;
                while (crackState.ProxyClocks < system.ProxyClocks)
                {
                    // If server happened to go down in between, escape.
                    if (Shell.Target == null || !TryGetSystem(system.Address, out _, out _))
                    {
                        Write("Error: connection to server lost\n");
                        yield break;
                    }

                    if (_signalWaiter.Trapped)
                    {
                        if (first)
                        {
                            Write("\n" +
                                  "»»»»   ERROR: OVERLOAD FAILED   ««««\n" +
                                  "»»     REMOTE TRAP TRIGGERED      ««\n" +
                                  "»»    MEMORY OVERFLOW DETECTED    ««\n" +
                                  "»»»» USER INTERVENTION REQUIRED ««««\n");
                            first = false;
                        }

                        Memory += (int)(ServerConstants.ForkbombRate * (World.Time - World.PreviousTime));
                        int tf = (int)(100.0 * System.GetUsedMemory() / System.SystemMemory) / 25;
                        if (tf > warningGate)
                        {
                            Write($"\nMEMORY {tf * 25}% CONSUMED\n");
                            warningGate = tf;
                        }

                        yield return null;
                    }
                    else
                        crackState.ProxyClocks += shellSystem.ClockSpeed * (World.Time - World.PreviousTime);

                    yield return null;
                }

                Write("\n«««« PROXY OVERLOAD COMPLETE »»»»\n");
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

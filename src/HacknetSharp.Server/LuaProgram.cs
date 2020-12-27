using System.Collections.Generic;
using System.Collections.Immutable;
using MoonSharp.Interpreter;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a lua-function-backed program.
    /// </summary>
    [IgnoreRegistration]
    public class LuaProgram : Program
    {
        private static readonly IReadOnlyDictionary<string, object> _defaultDict =
            ImmutableDictionary.Create<string, object>();

        private readonly Coroutine _coroutine;
        private readonly IReadOnlyDictionary<string, object> _additionalProps;

        /// <summary>
        /// Creates a lua program from the specified coroutine.
        /// </summary>
        /// <param name="coroutine">Coroutine to use.</param>
        /// <param name="additionalProps">Additional properties to initialize with.</param>
        public LuaProgram(Coroutine coroutine, IReadOnlyDictionary<string, object>? additionalProps = null)
        {
            _coroutine = coroutine;
            _additionalProps = additionalProps ?? _defaultDict;
        }

        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            var manager = World.ScriptManager;
            while (_coroutine.State != CoroutineState.Dead)
            {
                manager.SetGlobal("system", System);
                manager.SetGlobal("self", this);
                manager.SetGlobal("login", Login);
                manager.SetGlobal("argv", Argv);
                manager.SetGlobal("argc", Argv.Length);
                manager.SetGlobal("args", Context.Args);
                manager.SetGlobal("shell", Shell);
                manager.SetGlobal("pwd", Shell.WorkingDirectory);
                manager.SetGlobal("me", Person);
                try
                {
                    foreach (var (k, v) in _additionalProps)
                        manager.SetGlobal(k, v);
                    DynValue? result = _coroutine.Resume();
                    if (_coroutine.State == CoroutineState.Suspended)
                        yield return result?.ToObject() as YieldToken;
                }
                finally
                {
                    manager.ClearGlobal("system");
                    manager.ClearGlobal("self");
                    manager.ClearGlobal("login");
                    manager.ClearGlobal("argv");
                    manager.ClearGlobal("argc");
                    manager.ClearGlobal("args");
                    manager.ClearGlobal("shell");
                    manager.ClearGlobal("pwd");
                    manager.ClearGlobal("me");
                    foreach (var (k, _) in _additionalProps)
                        try
                        {
                            manager.ClearGlobal(k);
                        }
                        catch
                        {
                            // ignored
                        }
                }
            }
        }
    }
}

using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a lua-function-backed service.
    /// </summary>
    [IgnoreRegistration]
    public class LuaService : Service
    {
        private readonly Coroutine _coroutine;

        /// <summary>
        /// Creates a lua service from the specified coroutine.
        /// </summary>
        /// <param name="coroutine">Coroutine to use.</param>
        public LuaService(Coroutine coroutine)
        {
            _coroutine = coroutine;
        }

        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            var manager = World.ScriptManager;
            while (_coroutine.State != CoroutineState.Dead)
            {
                manager.SetGlobal("world", World);
                manager.SetGlobal("system", System);
                manager.SetGlobal("self", this);
                manager.SetGlobal("login", Login);
                manager.SetGlobal("argv", Argv);
                manager.SetGlobal("argc", Argv.Length);
                manager.SetGlobal("args", Context.Args);
                try
                {
                    DynValue? result = _coroutine.Resume();
                    if (_coroutine.State == CoroutineState.Suspended)
                        yield return result?.ToObject() as YieldToken;
                }
                finally
                {
                    manager.ClearGlobal("world");
                    manager.ClearGlobal("system");
                    manager.ClearGlobal("self");
                    manager.ClearGlobal("login");
                    manager.ClearGlobal("argv");
                    manager.ClearGlobal("argc");
                    manager.ClearGlobal("args");
                }
            }
        }
    }
}

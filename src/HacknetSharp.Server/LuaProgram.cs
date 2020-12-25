using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a lua-function-backed program.
    /// </summary>
    [IgnoreRegistration]
    public class LuaProgram : Program
    {
        private readonly Coroutine _coroutine;

        /// <summary>
        /// Creates a lua program from the specified coroutine.
        /// </summary>
        /// <param name="coroutine">Coroutine to use.</param>
        public LuaProgram(Coroutine coroutine)
        {
            _coroutine = coroutine;
        }

        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            while (_coroutine.State != CoroutineState.Dead)
            {
                World.ScriptManager.SetGlobal("world", World);
                World.ScriptManager.SetGlobal("system", System);
                World.ScriptManager.SetGlobal("self", this);
                World.ScriptManager.SetGlobal("login", Login);
                World.ScriptManager.SetGlobal("argv", Argv);
                World.ScriptManager.SetGlobal("argc", Argv.Length);
                World.ScriptManager.SetGlobal("args", Context.Args);
                World.ScriptManager.SetGlobal("shell", Shell);
                World.ScriptManager.SetGlobal("pwd", Shell.WorkingDirectory);
                World.ScriptManager.SetGlobal("me", Person);
                try
                {
                    DynValue? result;
                    result = _coroutine.Resume();
                    if (_coroutine.State == CoroutineState.Suspended)
                        yield return result?.ToObject() as YieldToken;
                }
                finally
                {
                    World.ScriptManager.ClearGlobal("world");
                    World.ScriptManager.ClearGlobal("system");
                    World.ScriptManager.ClearGlobal("self");
                    World.ScriptManager.ClearGlobal("login");
                    World.ScriptManager.ClearGlobal("argv");
                    World.ScriptManager.ClearGlobal("argc");
                    World.ScriptManager.ClearGlobal("args");
                    World.ScriptManager.ClearGlobal("shell");
                    World.ScriptManager.ClearGlobal("pwd");
                    World.ScriptManager.ClearGlobal("me");
                }
            }
        }
    }
}

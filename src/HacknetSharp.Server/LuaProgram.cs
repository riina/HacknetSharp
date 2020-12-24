using System;
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
                var result = _coroutine.Resume();
                if (_coroutine.State == CoroutineState.Suspended)
                    yield return result?.ToObject() as YieldToken;
            }
        }
    }
}

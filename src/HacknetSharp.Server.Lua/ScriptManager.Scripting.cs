using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using MoonSharp.Interpreter;

namespace HacknetSharp.Server.Lua
{
    public partial class ScriptManager
    {
        #region Script functions

        /// <summary>
        /// Add globals to a dictionary from an object.
        /// </summary>
        /// <param name="obj">Object to get globals from.</param>
        /// <param name="globals">Globals dictionary.</param>
        public static void LoadGlobalsFromObject(object obj, Dictionary<string, object> globals)
        {
            var type = obj.GetType();
            foreach (var method in
                     type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                try
                {
                    var attr = method.GetCustomAttributes(typeof(GlobalAttribute)).FirstOrDefault() as GlobalAttribute;
                    if (attr == null) continue;
                    string name = attr.Name ?? method.Name;
                    globals[name] = method.CreateDelegate(attr.DelegateType, obj);
                }
                catch (Exception e)
                {
                    throw new AggregateException($"Error thrown while processing method {method.Name} on {type.Name}",
                        e);
                }
        }

        /// <summary>
        /// Register globals.
        /// </summary>
        /// <param name="globals">Globals to register.</param>
        public void AddGlobals(Dictionary<string, object> globals)
        {
            foreach (var (key, value) in globals)
            {
                _script.Globals[key] = value;
                _globals.Add(key);
            }
        }

        /// <summary>
        /// Sets value in global table.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void SetGlobal(string key, object? value)
            => _script.Globals[key] = value;

        /// <summary>
        /// Gets value from global table.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Value.</returns>
        public DynValue? GetGlobal(string key) => _script.Globals.Get(key);

        /// <summary>
        /// Gets value from global table.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Value.</returns>
        public TValue? GetGlobalAs<TValue>(string key)
        {
            var res = _script.Globals.Get(key).ToObject();
            try
            {
                return res is TValue || res.GetType().IsValueType ? (TValue)res : default;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Clears value in global table.
        /// </summary>
        /// <param name="key">Key.</param>
        public void ClearGlobal(string key)
            => _script.Globals[key] = DynValue.Nil;

        /// <summary>
        /// Executes and returns the raw result of a raw lua script.
        /// </summary>
        /// <param name="script">Script.</param>
        /// <param name="errorToString">If true, returns error as string.</param>
        /// <returns>Result.</returns>
        [Global(typeof(Func<string, bool, DynValue>))]
        public DynValue EvaluateScript(string script, bool errorToString = false)
        {
            try
            {
                return _script.DoString(script);
            }
            catch (Exception e)
            {
                if (!errorToString) throw;
                return DynValue.FromObject(_script, e.ToString());
            }
        }

        /// <summary>
        /// Creates a dynamic expression.
        /// </summary>
        /// <param name="expression">Expression.</param>
        /// <returns>Expression.</returns>
        public DynamicExpression CreateDynamicExpression(string expression)
        {
            return _script.CreateDynamicExpression(expression);
        }

        /// <summary>
        /// Evaluates an expression.
        /// </summary>
        /// <param name="expression">Expression.</param>
        /// <param name="errorToString">If true, returns error as string.</param>
        /// <returns>Result.</returns>
        [Global(typeof(Func<string, bool, DynValue>))]
        public DynValue EvaluateExpression(string expression, bool errorToString = false)
        {
            try
            {
                return CreateDynamicExpression(expression).Evaluate();
            }
            catch (Exception e)
            {
                if (!errorToString) throw;
                return DynValue.FromObject(_script, e.ToString());
            }
        }

        /// <summary>
        /// Gets string from a dynvalue.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <returns>String</returns>
        [Global(typeof(Func<DynValue, string>))]
        public string ToString(DynValue value)
        {
            var obj = value.ToObject();
            return obj is string str ? str : obj.ToString() ?? "<object>";
        }

        /// <summary>
        /// Registers a raw lua expression.
        /// </summary>
        /// <param name="key">Script key.</param>
        /// <param name="expression">Expression.</param>
        /// <returns>New value or existing value.</returns>
        public DynValue RegisterExpression(string key, string expression)
        {
            if (!_expressions.TryGetValue(key, out var dyn))
                _expressions[key] = dyn = _script.DoString(expression);
            return dyn;
        }

        /// <summary>
        /// Registers a raw lua expression.
        /// </summary>
        /// <param name="key">Script key.</param>
        /// <param name="stream">Expression stream.</param>
        /// <returns>New value or existing value.</returns>
        public DynValue RegisterExpression(string key, Stream stream)
        {
            if (!_expressions.TryGetValue(key, out var dyn))
                _expressions[key] = dyn = _script.DoStream(stream);
            return dyn;
        }

        /// <summary>
        /// Attempts to retrieve registered expression.
        /// </summary>
        /// <param name="key">Script key.</param>
        /// <param name="expression">Obtained expression.</param>
        /// <returns>True if found.</returns>
        public bool TryGetExpression(string key, [NotNullWhen(true)] out DynValue? expression)
        {
            return _expressions.TryGetValue(key, out expression);
        }

        /// <summary>
        /// Creates a coroutine.
        /// </summary>
        /// <param name="fun">Target function.</param>
        /// <returns>Coroutine.</returns>
        public Coroutine GetCoroutine(DynValue fun)
        {
            return _script.CreateCoroutine(fun).Coroutine;
        }

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        public void RegisterAction(string name, Action action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        public void RegisterAction<T1>(string name, Action<T1> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        public void RegisterAction<T1, T2>(string name, Action<T1, T2> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3>(string name, Action<T1, T2, T3> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4>(string name, Action<T1, T2, T3, T4> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5>(string name, Action<T1, T2, T3, T4, T5> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6>(string name, Action<T1, T2, T3, T4, T5, T6> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="T14">14th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="T14">14th type parameter.</typeparam>
        /// <typeparam name="T15">15th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="action">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="T14">14th type parameter.</typeparam>
        /// <typeparam name="T15">15th type parameter.</typeparam>
        /// <typeparam name="T16">16th type parameter.</typeparam>
        public void RegisterAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string name,
            Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> action) =>
            _script.Globals[name] = action;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<TR>(string name, Func<TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, TR>(string name, Func<T1, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, TR>(string name, Func<T1, T2, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, TR>(string name, Func<T1, T2, T3, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, TR>(string name, Func<T1, T2, T3, T4, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, TR>(string name, Func<T1, T2, T3, T4, T5, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="T14">14th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="T14">14th type parameter.</typeparam>
        /// <typeparam name="T15">15th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Registers a delegate for use inside the environment.
        /// </summary>
        /// <param name="name">Function name.</param>
        /// <param name="function">Function delegate.</param>
        /// <typeparam name="T1">1st type parameter.</typeparam>
        /// <typeparam name="T2">2nd type parameter.</typeparam>
        /// <typeparam name="T3">3rd type parameter.</typeparam>
        /// <typeparam name="T4">4th type parameter.</typeparam>
        /// <typeparam name="T5">5th type parameter.</typeparam>
        /// <typeparam name="T6">6th type parameter.</typeparam>
        /// <typeparam name="T7">7th type parameter.</typeparam>
        /// <typeparam name="T8">8th type parameter.</typeparam>
        /// <typeparam name="T9">9th type parameter.</typeparam>
        /// <typeparam name="T10">10th type parameter.</typeparam>
        /// <typeparam name="T11">11th type parameter.</typeparam>
        /// <typeparam name="T12">12th type parameter.</typeparam>
        /// <typeparam name="T13">13th type parameter.</typeparam>
        /// <typeparam name="T14">14th type parameter.</typeparam>
        /// <typeparam name="T15">15th type parameter.</typeparam>
        /// <typeparam name="T16">16th type parameter.</typeparam>
        /// <typeparam name="TR">Return type.</typeparam>
        public void RegisterFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR>(
            string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TR> function) =>
            _script.Globals[name] = function;

        /// <summary>
        /// Runs a function by name.
        /// </summary>
        /// <param name="name">Function to execute.</param>
        public void RunVoidFunctionByName(string name)
        {
            var fn = _script.Globals.Get(name);
            if (Equals(fn, DynValue.Nil)) return;
            _script.Call(fn);
        }

        /// <summary>
        /// Runs a script.
        /// </summary>
        /// <param name="script">Script to execute.</param>
        public void RunVoidScript(DynValue script)
        {
            _script.Call(script);
        }

        /// <summary>
        /// Runs a raw lua script.
        /// </summary>
        /// <param name="script">Script raw contents.</param>
        public void RunVoidScript(string script)
        {
            _script.DoString(script);
        }

        /// <summary>
        /// Runs a function by name with specified arguments.
        /// </summary>
        /// <param name="name">Function to execute.</param>
        /// <param name="args">Arguments to pass.</param>
        public void RunVoidFunctionByName(string name, params object[] args)
        {
            var fn = _script.Globals.Get(name);
            if (Equals(fn, DynValue.Nil)) return;
            _script.Call(fn, args);
        }

        /// <summary>
        /// Runs a script with specified arguments.
        /// </summary>
        /// <param name="script">Script to execute.</param>
        /// <param name="args">Arguments to pass.</param>
        public void RunVoidScript(DynValue script, params object[] args)
        {
            _script.Call(script, args);
        }

        /// <summary>
        /// Runs a raw lua script and gets the result.
        /// </summary>
        /// <param name="script">Script raw contents.</param>
        /// <returns>Retrieved object.</returns>
        public TR? RunScript<TR>(string script)
        {
            return DynValueToTarget<TR>(_script.DoString(script));
        }

        /// <summary>
        /// Runs a script and gets the result.
        /// </summary>
        /// <param name="script">Script to execute.</param>
        /// <typeparam name="TR">Return type.</typeparam>
        /// <returns>Retrieved object.</returns>
        public TR? RunScript<TR>(DynValue script)
        {
            return DynValueToTarget<TR>(_script.Call(script));
        }

        /// <summary>
        /// Runs a script with specified arguments and gets the result.
        /// </summary>
        /// <param name="script">Script to execute.</param>
        /// <param name="args">Arguments to pass.</param>
        /// <typeparam name="TR">Return type.</typeparam>
        /// <returns>Retrieved object.</returns>
        public TR? RunScript<TR>(DynValue script, params object[] args)
        {
            return DynValueToTarget<TR>(_script.Call(script, args));
        }

        private TR? DynValueToTarget<TR>(DynValue value)
        {
            if (typeof(TR) == typeof(DynValue)) return (TR)(object)value;
            var res = value.ToObject();
            try
            {
                return res is TR || res.GetType().IsValueType ? (TR)res : default;
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Runs a function by name and gets the result.
        /// </summary>
        /// <param name="name">Function to execute.</param>
        /// <typeparam name="TR">Return type.</typeparam>
        /// <returns>Retrieved object.</returns>
        public TR? RunFunctionByName<TR>(string name)
        {
            var fn = _script.Globals.Get(name);
            if (Equals(fn, DynValue.Nil)) return default;
            return DynValueToTarget<TR>(_script.Call(fn));
        }

        /// <summary>
        /// Runs a function by name with specified arguments and gets the result.
        /// </summary>
        /// <param name="name">Function to execute.</param>
        /// <param name="args">Arguments to pass.</param>
        /// <typeparam name="TR">Return type.</typeparam>
        /// <returns>Retrieved object.</returns>
        public TR? RunFunctionByName<TR>(string name, params object[] args)
        {
            var fn = _script.Globals.Get(name);
            if (Equals(fn, DynValue.Nil)) return default;
            return DynValueToTarget<TR>(_script.Call(fn, args));
        }

        #endregion
    }
}

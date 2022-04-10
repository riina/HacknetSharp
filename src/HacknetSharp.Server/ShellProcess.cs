using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.CoreServices;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a running shell.
    /// </summary>
    public class ShellProcess : Process
    {
        /// <summary>
        /// Program context of this shell.
        /// </summary>
        public ProgramContext ProgramContext { get; }

        /// <summary>
        /// Working directory of this shell.
        /// </summary>
        public string WorkingDirectory { get; set; } = "/";

        /// <summary>
        /// If set true, shell process will exit on the next execution step.
        /// </summary>
        /// <remarks>
        /// It's better to call <see cref="IWorld.CompleteRecurse"/> instead.
        /// </remarks>
        public bool Close { get; set; }

        /// <summary>
        /// Shell variables.
        /// </summary>
        private readonly Dictionary<string, string> _variables = new();

        private readonly Dictionary<string, Func<string>> _builtinVariables;

        /// <summary>
        /// Crack states by system address.
        /// </summary>
        public Dictionary<uint, CrackState> CrackStates { get; set; } = new();

        /// <summary>
        /// Gets crack state for target system.
        /// </summary>
        /// <param name="target">Target system.</param>
        /// <returns>Crack state.</returns>
        public CrackState GetCrackState(SystemModel target)
        {
            if (!CrackStates.TryGetValue(target.Address, out var res))
                CrackStates[target.Address] = res = new CrackState(ProcessContext.System, target);
            target.UpdateCrackState(res);
            return res;
        }

        /// <summary>
        /// Contains information on crack state for a specified target.
        /// </summary>
        public class CrackState
        {
            /// <summary>
            /// Source system.
            /// </summary>
            public SystemModel Source { get; }

            /// <summary>
            /// Target system.
            /// </summary>
            public SystemModel Target { get; }

            /// <summary>
            /// Open vulnerabilities on target.
            /// </summary>
            public Dictionary<VulnerabilityModel, int> OpenVulnerabilities { get; }

            /// <summary>
            /// Firewall solution.
            /// </summary>
            public string FirewallSolution { get; set; }

            /// <summary>
            /// Firewall iterations.
            /// </summary>
            public int FirewallIterations { get; set; }

            /// <summary>
            /// Firewall solve state.
            /// </summary>
            public bool FirewallSolved { get; set; }

            /// <summary>
            /// CPU cycles spent cracking proxy.
            /// </summary>
            public double ProxyClocks { get; set; }

            internal int FirewallVersion { get; set; }

            internal int ProxyVersion { get; set; }

            /// <summary>
            /// Creates a new instance of <see cref="CrackState"/>.
            /// </summary>
            /// <param name="source">Source system.</param>
            /// <param name="target">Target system.</param>
            public CrackState(SystemModel source, SystemModel target)
            {
                Source = source;
                Target = target;
                OpenVulnerabilities = new Dictionary<VulnerabilityModel, int>();
                FirewallSolution = "";
                FirewallVersion = -1;
                ProxyVersion = -1;
            }

            /// <summary>
            /// Opens the specified vulnerability.
            /// </summary>
            /// <param name="vulnerability">Vulnerability to open.</param>
            public void OpenVulnerability(VulnerabilityModel vulnerability)
            {
                Target.OpenVulnerability(this, vulnerability);
            }
        }

        /// <summary>
        /// Processes with remote shells by system address.
        /// </summary>
        public Dictionary<uint, ProgramProcess> Remotes { get; set; } = new();

        /// <summary>
        /// Target system for login / hacking.
        /// </summary>
        public SystemModel? Target { get; set; }

        /// <summary>
        /// Active chat service for this shell.
        /// </summary>
        public ChatService? Chat { get; set; }

        /// <summary>
        /// Active chat room for this shell.
        /// </summary>
        public string? ChatRoom { get; set; }

        /// <summary>
        /// Active chat name for this shell.
        /// </summary>
        public string? ChatName { get; set; }

        /// <summary>
        /// Shell for which this shell is a remote.
        /// </summary>
        public ShellProcess? RemoteParent { get; set; }

        private bool _cleaned;

        /*public IEnumerable<string> AllVariables
        {
            get
            {
                var en = (IEnumerable<string>)Variables.Values;
                /*int shIdx = ProgramContext.Person.ShellChain.IndexOf(ProgramContext.Shell);
                foreach (var sh in ProgramContext.Person.ShellChain.Take(shIdx).Reverse())
                    en = en.Concat(sh.Variables.Values);#1#
                return en;
            }
        }*/

        /// <summary>
        /// Creates a new instance of <see cref="ShellProcess"/>.
        /// </summary>
        /// <param name="context">Program context.</param>
        public ShellProcess(ProgramContext context) : base(context)
        {
            ProgramContext = context;
            _builtinVariables = new Dictionary<string, Func<string>> { ["PWD"] = () => WorkingDirectory, ["USER"] = () => ProcessContext.Login.User };
        }

        /// <inheritdoc />
        public override bool Update(IWorld world)
        {
            if (Close)
            {
                world.CompleteRecurse(this, CompletionKind.Normal);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get all variables.
        /// </summary>
        /// <returns>All variables.</returns>
        public IEnumerable<KeyValuePair<string, string>> GetVariables()
        {
            return _builtinVariables
                .Select(kvp => new KeyValuePair<string, string>(kvp.Key, kvp.Value()))
                .Concat(_variables);
        }

        /// <summary>
        /// Sets a variable.
        /// </summary>
        /// <param name="key">Variable name.</param>
        /// <param name="value">Variable value.</param>
        public void SetVariable(string key, string value)
        {
            if (!_builtinVariables.ContainsKey(key))
                _variables[key] = value;
        }

        /// <summary>
        /// Removes specified variable.
        /// </summary>
        /// <param name="key">Variable name.</param>
        public void RemoveVariable(string key)
        {
            _variables.Remove(key);
        }

        /// <summary>
        /// Attempts to get a variable.
        /// </summary>
        /// <param name="key">Variable name.</param>
        /// <param name="value">Obtained value if successful.</param>
        /// <returns>True if variable was retrieved.</returns>
        public bool TryGetVariable(string key, [NotNullWhen(true)] out string? value)
        {
            if (_builtinVariables.TryGetValue(key, out var tmp))
            {
                value = tmp();
                return true;
            }

            if (_variables.TryGetValue(key, out value))
                return true;
            /*int shIdx = ProgramContext.Person.ShellChain.IndexOf(ProgramContext.Shell);
            foreach (var sh in ProgramContext.Person.ShellChain.Take(shIdx).Reverse())
                if (sh.Variables.TryGetValue(key, out value))
                    return true;*/
            return false;
        }

        /// <inheritdoc />
        public override bool Complete(CompletionKind completionKind)
        {
            if (_cleaned) return true;
            _cleaned = true;
            Completed = completionKind;
            if (completionKind != CompletionKind.Normal)
            {
                ProgramContext.User.WriteEventSafe(Program.Output("[Shell terminated]\n"));
                ProgramContext.User.FlushSafeAsync();
            }

            if (ProgramContext.Person.ShellChain.Count == 0)
            {
                ProgramContext.User.WriteEventSafe(new ServerDisconnectEvent { Reason = "Disconnected by server." });
                ProgramContext.User.FlushSafeAsync();
            }

            Program.SignalUnbindProcess(ProgramContext, this);
            return true;
        }
    }
}

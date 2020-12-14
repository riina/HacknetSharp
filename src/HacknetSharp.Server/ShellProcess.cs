using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HacknetSharp.Events.Server;
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
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Open vulnerabilities by system address.
        /// </summary>
        public Dictionary<uint, HashSet<VulnerabilityModel>> OpenVulnerabilities { get; set; } =
            new Dictionary<uint, HashSet<VulnerabilityModel>>();

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
        /// Attempts to get a variable.
        /// </summary>
        /// <param name="key">Variable name.</param>
        /// <param name="value">Obtained value if successful.</param>
        /// <returns>True if variable was retrieved.</returns>
        public bool TryGetVariable(string key, [NotNullWhen(true)] out string? value)
        {
            if (Variables.TryGetValue(key, out value))
                return true;
            /*int shIdx = ProgramContext.Person.ShellChain.IndexOf(ProgramContext.Shell);
            foreach (var sh in ProgramContext.Person.ShellChain.Take(shIdx).Reverse())
                if (sh.Variables.TryGetValue(key, out value))
                    return true;*/
            return false;
        }

        /// <inheritdoc />
        public override void Complete(CompletionKind completionKind)
        {
            if (_cleaned) return;
            _cleaned = true;
            Completed = completionKind;
            if (completionKind != CompletionKind.Normal)
            {
                ProgramContext.User.WriteEventSafe(Program.Output("[Shell terminated]"));
                ProgramContext.User.FlushSafeAsync();
            }

            if (ProgramContext.Person.ShellChain.Count == 0)
            {
                ProgramContext.User.WriteEventSafe(new ServerDisconnectEvent {Reason = "Disconnected by server."});
                ProgramContext.User.FlushSafeAsync();
            }

            Program.SignalUnbindProcess(ProgramContext, this);
        }
    }
}

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HacknetSharp.Events.Server;

namespace HacknetSharp.Server
{
    public class ShellProcess : Process
    {
        public ProgramContext ProgramContext { get; }
        public string WorkingDirectory { get; set; } = "/";
        public bool Close { get; set; }
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
        private bool _cleaned;

        public IEnumerable<string> AllVariables
        {
            get
            {
                var en = (IEnumerable<string>)Variables.Values;
                /*int shIdx = ProgramContext.Person.ShellChain.IndexOf(ProgramContext.Shell);
                foreach (var sh in ProgramContext.Person.ShellChain.Take(shIdx).Reverse())
                    en = en.Concat(sh.Variables.Values);*/
                return en;
            }
        }

        public ShellProcess(ProgramContext context) : base(context)
        {
            ProgramContext = context;
        }

        public override bool Update(IWorld world)
        {
            if (Close)
            {
                world.CompleteRecurse(this, CompletionKind.Normal);
                return true;
            }

            return false;
        }

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

        public override void Complete(CompletionKind completionKind)
        {
            Clean(completionKind);
        }

        private void Clean(CompletionKind completionKind)
        {
            if (_cleaned) return;
            _cleaned = true;
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
        }
    }
}

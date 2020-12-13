using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HacknetSharp.Events.Server;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server
{
    public class ShellProcess : Process
    {
        public ProgramContext ProgramContext { get; }
        public string WorkingDirectory { get; set; } = "/";
        public bool Close { get; set; }
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

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

        public bool TryGetTarget([NotNullWhen(true)] out SystemModel? target)
        {
            if (!Variables.TryGetValue("TARGET", out string? addr) ||
                !IPAddressRange.TryParse(addr, false, out var ip) ||
                !ip.TryGetIPv4HostAndSubnetMask(out uint host, out _))
            {
                target = null;
                return false;
            }

            target = ProgramContext.World.Model.Systems.FirstOrDefault(s => s.Address == host);
            return target != null;
        }

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

            ServerUtil.SignalUnbindProcess(ProgramContext, this);
        }
    }
}

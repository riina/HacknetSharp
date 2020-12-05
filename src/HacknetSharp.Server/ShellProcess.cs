using HacknetSharp.Events.Server;

namespace HacknetSharp.Server
{
    public class ShellProcess : Process
    {
        public ProgramContext ProgramContext { get; }
        public string WorkingDirectory { get; set; } = "/";
        public bool Close { get; set; }
        private bool _cleaned;

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

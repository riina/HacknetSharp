namespace HacknetSharp.Server
{
    public class ShellProcess : Process
    {
        public ProgramContext ProgramContext { get; }
        public bool Disconnect { get; set; }
        private bool _cleaned;

        public ShellProcess(ProgramContext context) : base(context)
        {
            ProgramContext = context;
        }

        public override bool Update(IWorld world)
        {
            if (!Disconnect) return false;
            Clean();
            return true;
        }

        public override void Kill()
        {
            // TODO implement kill procedure: message to client
            Clean();
        }

        private void Clean()
        {
            if (_cleaned) return;
            _cleaned = true;
        }
    }
}

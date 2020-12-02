namespace HacknetSharp.Server.Common
{
    public class ExecutableContext
    {
        public IWorld World { get; set; } = null!;
        public System System { get; set; } = null!;
        public string[] Argv { get; set; } = null!;
    }
}

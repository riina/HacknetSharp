using HacknetSharp.Server.Models;

namespace HacknetSharp.Server
{
    public class ProcessContext
    {
        public uint ParentPid { get; set; }
        public uint Pid { get; set; }
        public IWorld World { get; set; } = null!;
        public SystemModel System { get; set; } = null!;
        public string[] Argv { get; set; } = null!;
    }
}

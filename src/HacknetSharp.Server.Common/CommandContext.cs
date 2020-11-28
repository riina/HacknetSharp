using System;

namespace HacknetSharp.Server.Common
{
    public class CommandContext
    {
        public IWorld World { get; set; } = null!;
        public IPersonContext Person { get; set; } = null!;
        public System System { get; set; } = null!;
        public Guid OperationId { get; set; }
        public bool Disconnect { get; set; }
        public string[] Argv { get; set; } = null!;
    }
}

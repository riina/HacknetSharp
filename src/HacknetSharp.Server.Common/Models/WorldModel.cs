using System;

namespace HacknetSharp.Server.Common.Models
{
    public class WorldModel : Model<Guid>
    {
        public virtual string Name { get; set; } = null!;
        public virtual string SystemTemplate { get; set; } = null!;
        public virtual string StartupProgram { get; set; } = null!;
        public virtual string StartupCommandLine { get; set; } = null!;
    }
}

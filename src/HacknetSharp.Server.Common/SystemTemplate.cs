using System.Collections.Generic;

namespace HacknetSharp.Server.Common
{
    public class SystemTemplate
    {
        public string? OsName { get; set; }
        public List<string>? Users { get; set; }
        public List<string>? Filesystem { get; set; }
    }
}

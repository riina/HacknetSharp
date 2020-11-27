using System.Collections.Generic;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public class SystemTemplate
    {
        public string? OsName { get; set; }
        public List<string>? Users { get; set; }
        public List<string>? Filesystem { get; set; }

        public void ApplyTemplate(SystemModel model)
        {
            // TODO apply template
        }
    }
}

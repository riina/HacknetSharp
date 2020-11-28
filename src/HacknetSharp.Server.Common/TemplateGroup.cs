using System.Collections.Generic;
using HacknetSharp.Server.Common.Templates;

namespace HacknetSharp.Server.Common
{
    public class TemplateGroup
    {
        public Dictionary<string, WorldTemplate> WorldTemplates { get; }
        public Dictionary<string, PersonTemplate> PersonTemplates { get; }
        public Dictionary<string, SystemTemplate> SystemTemplates { get; }

        public TemplateGroup()
        {
            WorldTemplates = new Dictionary<string, WorldTemplate>();
            PersonTemplates = new Dictionary<string, PersonTemplate>();
            SystemTemplates = new Dictionary<string, SystemTemplate>();
        }
    }
}

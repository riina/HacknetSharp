using System.Collections.Generic;

namespace HacknetSharp.Server
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

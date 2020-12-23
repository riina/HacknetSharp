using System.Collections.Generic;
using HacknetSharp.Server.Templates;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a collection of templates.
    /// </summary>
    public class TemplateGroup
    {
        /// <summary>
        /// World templates.
        /// </summary>
        public Dictionary<string, WorldTemplate> WorldTemplates { get; }

        /// <summary>
        /// Person templates.
        /// </summary>
        public Dictionary<string, PersonTemplate> PersonTemplates { get; }

        /// <summary>
        /// System templates.
        /// </summary>
        public Dictionary<string, SystemTemplate> SystemTemplates { get; }

        /// <summary>
        /// Mission templates.
        /// </summary>
        public Dictionary<string, MissionTemplate> MissionTemplates { get; }

        /// <summary>
        /// Creates an empty instance of <see cref="TemplateGroup"/>.
        /// </summary>
        public TemplateGroup()
        {
            WorldTemplates = new Dictionary<string, WorldTemplate>();
            PersonTemplates = new Dictionary<string, PersonTemplate>();
            SystemTemplates = new Dictionary<string, SystemTemplate>();
            MissionTemplates = new Dictionary<string, MissionTemplate>();
        }
    }
}

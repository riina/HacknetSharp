using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Stores configuration information.
    /// </summary>
    public class ServerConfigBase
    {
        /// <summary>
        /// Program types.
        /// </summary>
        public HashSet<Type> Programs { get; set; }

        /// <summary>
        /// Service types.
        /// </summary>
        public HashSet<Type> Services { get; set; }

        /// <summary>
        /// Default world.
        /// </summary>
        public string? DefaultWorld { get; set; }

        /// <summary>
        /// World templates.
        /// </summary>
        public TemplateGroup Templates { get; set; }

        /// <summary>
        /// Message of the day.
        /// </summary>
        public string? Motd { get; set; }

        /// <summary>
        /// Log receiver.
        /// </summary>
        public ILogger? Logger { get; set; }

        /// <summary>
        /// Creates new instance of <see cref="ServerConfigBase"/>
        /// </summary>
        public ServerConfigBase()
        {
            Programs = new HashSet<Type>();
            Services = new HashSet<Type>();
            Templates = new TemplateGroup();
        }

        /// <summary>
        /// Adds programs.
        /// </summary>
        /// <param name="programs">Program types.</param>
        /// <returns>This config.</returns>
        public ServerConfigBase WithPrograms(IEnumerable<Type> programs)
        {
            Programs.UnionWith(programs);
            return this;
        }

        /// <summary>
        /// Adds services.
        /// </summary>
        /// <param name="services">Service types.</param>
        /// <returns>This config.</returns>
        public ServerConfigBase WithServices(IEnumerable<Type> services)
        {
            Services.UnionWith(services);
            return this;
        }

        /// <summary>
        /// Adds programs.
        /// </summary>
        /// <param name="programs">Program types.</param>
        /// <returns>This config.</returns>
        public ServerConfigBase WithPrograms(IEnumerable<IEnumerable<Type>> programs)
        {
            Programs.UnionWith(programs.SelectMany(x => x));
            return this;
        }

        /// <summary>
        /// Sets default world.
        /// </summary>
        /// <param name="defaultWorld">Default world.</param>
        /// <returns>This config.</returns>
        public ServerConfigBase WithDefaultWorld(string defaultWorld)
        {
            DefaultWorld = defaultWorld;
            return this;
        }

        /// <summary>
        /// Sets templates.
        /// </summary>
        /// <param name="templates">Templates.</param>
        /// <returns>This config.</returns>
        public ServerConfigBase WithTemplates(TemplateGroup templates)
        {
            Templates = templates;
            return this;
        }

        /// <summary>
        /// Sets message of the dsy.
        /// </summary>
        /// <param name="motd">Message of the day.</param>
        /// <returns>This config.</returns>
        public ServerConfigBase WithMotd(string? motd)
        {
            Motd = motd;
            return this;
        }

        /// <summary>
        /// Sets message of the dsy.
        /// </summary>
        /// <param name="logger">Log receiver.</param>
        /// <returns>This config.</returns>
        public ServerConfigBase WithLogger(ILogger? logger)
        {
            Logger = logger;
            return this;
        }
    }
}

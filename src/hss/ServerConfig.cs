﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using HacknetSharp.Server;
using HacknetSharp.Server.EF;
using Microsoft.Extensions.Logging;

namespace hss
{
    /// <summary>
    /// Stores configuration information.
    /// </summary>
    public class ServerConfig : ServerConfigBase
    {
        /// <summary>
        /// Storage context factory.
        /// </summary>
        public ServerDatabaseContextFactoryBase? StorageContextFactory { get; set; }

        /// <summary>
        /// Server certificate.
        /// </summary>
        public X509Certificate? Certificate { get; set; }

        /// <summary>
        /// TCP port.
        /// </summary>
        public ushort Port { get; set; }

        /// <summary>
        /// Creates new instance of <see cref="ServerConfig"/>
        /// </summary>
        public ServerConfig()
        {
            Programs = new HashSet<Type>();
            Services = new HashSet<Type>();
            Templates = new TemplateGroup();
        }

        /// <summary>
        /// Sets storage context factory.
        /// </summary>
        /// <param name="serverDatabaseContextFactory">Factory.</param>
        /// <returns>This config.</returns>
        public ServerConfig WithStorageContextFactory(ServerDatabaseContextFactoryBase serverDatabaseContextFactory)
        {
            StorageContextFactory = serverDatabaseContextFactory;
            return this;
        }

        /// <summary>
        /// Adds programs.
        /// </summary>
        /// <param name="programs">Program types.</param>
        /// <returns>This config.</returns>
        public new ServerConfig WithPrograms(IEnumerable<Type> programs)
        {
            Programs.UnionWith(programs);
            return this;
        }

        /// <summary>
        /// Adds services.
        /// </summary>
        /// <param name="services">Service types.</param>
        /// <returns>This config.</returns>
        public new ServerConfig WithServices(IEnumerable<Type> services)
        {
            Services.UnionWith(services);
            return this;
        }

        /// <summary>
        /// Adds programs.
        /// </summary>
        /// <param name="programs">Program types.</param>
        /// <returns>This config.</returns>
        public new ServerConfig WithPrograms(IEnumerable<IEnumerable<Type>> programs)
        {
            Programs.UnionWith(programs.SelectMany(x => x));
            return this;
        }

        /// <summary>
        /// Adds plugins.
        /// </summary>
        /// <param name="plugins">Plugin types.</param>
        /// <returns>This config.</returns>
        public new ServerConfig WithPlugins(IEnumerable<IEnumerable<Type>> plugins)
        {
            Plugins.UnionWith(plugins.SelectMany(x => x));
            return this;
        }

        /// <summary>
        /// Sets default world.
        /// </summary>
        /// <param name="defaultWorld">Default world.</param>
        /// <returns>This config.</returns>
        public new ServerConfig WithDefaultWorld(string defaultWorld)
        {
            DefaultWorld = defaultWorld;
            return this;
        }

        /// <summary>
        /// Sets templates.
        /// </summary>
        /// <param name="templates">Templates.</param>
        /// <returns>This config.</returns>
        public new ServerConfig WithTemplates(TemplateGroup templates)
        {
            Templates = templates;
            return this;
        }

        /// <summary>
        /// Sets TCP port.
        /// </summary>
        /// <param name="port">TCP port.</param>
        /// <returns>This config.</returns>
        public ServerConfig WithPort(ushort port)
        {
            Port = port;
            return this;
        }

        /// <summary>
        /// Sets server certificate.
        /// </summary>
        /// <param name="certificate">Server certificate.</param>
        /// <returns>This config.</returns>
        public ServerConfig WithCertificate(X509Certificate certificate)
        {
            Certificate = certificate;
            return this;
        }

        /// <summary>
        /// Sets message of the dsy.
        /// </summary>
        /// <param name="motd">Message of the day.</param>
        /// <returns>This config.</returns>
        public new ServerConfig WithMotd(string? motd)
        {
            Motd = motd;
            return this;
        }

        /// <summary>
        /// Sets message of the dsy.
        /// </summary>
        /// <param name="logger">Log receiver.</param>
        /// <returns>This config.</returns>
        public new ServerConfig WithLogger(ILogger? logger)
        {
            Logger = logger;
            return this;
        }

        /// <summary>
        /// Creates new <see cref="Server"/> instance using this configuration.
        /// </summary>
        /// <returns>Server instance.</returns>
        public Server CreateInstance() => new(this);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Stores configuration information.
    /// </summary>
    public class ServerConfig
    {
        /// <summary>
        /// Storage context factory.
        /// </summary>
        public StorageContextFactoryBase? StorageContextFactory { get; set; }

        /// <summary>
        /// Program types.
        /// </summary>
        public HashSet<Type> Programs { get; set; }

        /// <summary>
        /// Default world.
        /// </summary>
        public string? DefaultWorld { get; set; }

        /// <summary>
        /// World templates.
        /// </summary>
        public TemplateGroup Templates { get; set; }

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
            Templates = new TemplateGroup();
        }

        /// <summary>
        /// Sets storage context factory.
        /// </summary>
        /// <param name="storageContextFactory">Factory.</param>
        /// <returns>This config.</returns>
        public ServerConfig WithStorageContextFactory(StorageContextFactoryBase storageContextFactory)
        {
            StorageContextFactory = storageContextFactory;
            return this;
        }

        /// <summary>
        /// Adds programs.
        /// </summary>
        /// <param name="programs">Program types.</param>
        /// <returns>This config.</returns>
        public ServerConfig WithPrograms(IEnumerable<Type> programs)
        {
            Programs.UnionWith(programs);
            return this;
        }

        /// <summary>
        /// Adds programs.
        /// </summary>
        /// <param name="programs">Program types.</param>
        /// <returns>This config.</returns>
        public ServerConfig WithPrograms(IEnumerable<IEnumerable<Type>> programs)
        {
            Programs.UnionWith(programs.SelectMany(x => x));
            return this;
        }

        /// <summary>
        /// Sets default world.
        /// </summary>
        /// <returns>This config.</returns>
        /// <param name="defaultWorld">Default world.</param>
        public ServerConfig WithDefaultWorld(string defaultWorld)
        {
            DefaultWorld = defaultWorld;
            return this;
        }

        /// <summary>
        /// Sets templates.
        /// </summary>
        /// <returns>This config.</returns>
        /// <param name="templates">Templates.</param>
        public ServerConfig WithTemplates(TemplateGroup templates)
        {
            Templates = templates;
            return this;
        }

        /// <summary>
        /// Sets TCP port.
        /// </summary>
        /// <returns>This config.</returns>
        /// <param name="port">TCP port.</param>
        public ServerConfig WithPort(ushort port)
        {
            Port = port;
            return this;
        }

        /// <summary>
        /// Sets server certificate.
        /// </summary>
        /// <returns>Certificate.</returns>
        /// <param name="certificate">Server certificate.</param>
        public ServerConfig WithCertificate(X509Certificate certificate)
        {
            Certificate = certificate;
            return this;
        }

        /// <summary>
        /// Creates new <see cref="Server"/> instance using this configuration.
        /// </summary>
        /// <returns>Server instance.</returns>
        public Server CreateInstance() => new Server(this);
    }
}

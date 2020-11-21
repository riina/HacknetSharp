using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Stores configuration information.
    /// </summary>
    public class ServerConfig
    {
        /// <summary>
        /// Type for storage context factory.
        /// </summary>
        public Type? StorageContextFactoryType { get; set; }

        /// <summary>
        /// Program types.
        /// </summary>
        public HashSet<Type> Programs { get; set; }

        /// <summary>
        /// Default world.
        /// </summary>
        public string DefaultWorld  { get; set; }

        /// <summary>
        /// World templates.
        /// </summary>
        public HashSet<WorldTemplate> WorldTemplates { get; set; }

        /// <summary>
        /// System templates.
        /// </summary>
        public HashSet<SystemTemplate> SystemTemplates { get; set; }

        /// <summary>
        /// User access controller type.
        /// </summary>
        public Type? AccessControllerType { get; set; }

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
            StorageContextFactoryType = null;
            Programs = new HashSet<Type>();
            WorldTemplates = new HashSet<WorldTemplate>();
        }

        /// <summary>
        /// Sets factory type.
        /// </summary>
        /// <typeparam name="TFactory">Factory type.</typeparam>
        /// <returns>This config.</returns>
        public ServerConfig WithStorageContextFactory<TFactory>() where TFactory : StorageContextFactoryBase
        {
            StorageContextFactoryType = typeof(TFactory);
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
        /// Sets user access controller type.
        /// </summary>
        /// <typeparam name="T">User access controller type.</typeparam>
        /// <returns>This config.</returns>
        public ServerConfig WithAccessController<T>() where T : AccessController
        {
            AccessControllerType = typeof(T);
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
        /// Sets world templates.
        /// </summary>
        /// <returns>This config.</returns>
        /// <param name="worldTemplates">World templates.</param>
        public ServerConfig WithWorldTemplates(IEnumerable<WorldTemplate> worldTemplates)
        {
            WorldTemplates.UnionWith(worldTemplates);
            return this;
        }

        /// <summary>
        /// Sets system templates.
        /// </summary>
        /// <returns>This config.</returns>
        /// <param name="systemTemplates">System templates.</param>
        public ServerConfig WithSystemTemplates(IEnumerable<SystemTemplate> systemTemplates)
        {
            SystemTemplates.UnionWith(systemTemplates);
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
        /// Creates new <see cref="ServerInstance"/> instance using this configuration.
        /// </summary>
        /// <returns>Server instance.</returns>
        public ServerInstance CreateInstance() => new ServerInstance(this);
    }
}

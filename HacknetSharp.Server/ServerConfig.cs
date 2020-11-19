using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Model types.
        /// </summary>
        public HashSet<Type> Models { get; set; }

        /// <summary>
        /// Program types.
        /// </summary>
        public HashSet<Type> Programs { get; set; }

        /// <summary>
        /// World config files.
        /// </summary>
        public HashSet<WorldConfig> WorldConfigs { get; set; }

        /// <summary>
        /// User access controller type.
        /// </summary>
        public Type? AccessControllerType { get; set; }

        /// <summary>
        /// Creates new instance of <see cref="ServerConfig"/>
        /// </summary>
        public ServerConfig()
        {
            StorageContextFactoryType = null;
            Models = new HashSet<Type>(Util.DefaultModels);
            Programs = new HashSet<Type>(Util.DefaultPrograms);
            WorldConfigs = new HashSet<WorldConfig>();
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
        /// Adds models.
        /// </summary>
        /// <param name="models">Model types.</param>
        /// <returns>This config.</returns>
        public ServerConfig WithModels(IEnumerable<Type> models)
        {
            Models.UnionWith(models);
            return this;
        }

        /// <summary>
        /// Adds models.
        /// </summary>
        /// <param name="models">Model types.</param>
        /// <returns>This config.</returns>
        public ServerConfig WithModels(IEnumerable<IEnumerable<Type>> models)
        {
            Models.UnionWith(models.SelectMany(x => x));
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
        /// Sets world configurations.
        /// </summary>
        /// <returns>This config.</returns>
        /// <param name="worldConfigs">World configurations.</param>
        public ServerConfig WithWorldConfigs(IEnumerable<WorldConfig> worldConfigs)
        {
            WorldConfigs.UnionWith(worldConfigs);
            return this;
        }

        /// <summary>
        /// Creates new <see cref="ServerInstance"/> instance using this configuration.
        /// </summary>
        /// <returns>Server instance.</returns>
        public ServerInstance CreateInstance() => new ServerInstance(this);
    }
}

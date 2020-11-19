using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a database session.
    /// </summary>
    public class WorldStorageContext : DbContext
    {
        /// <summary>
        /// Delegate type for methods with <see cref="ModelBuilderCallbackAttribute"/>
        /// </summary>
        /// <param name="builder">The builder to use.</param>
        public delegate void ModelBuilderDelegate(ModelBuilder builder);

        private readonly List<ModelBuilderDelegate> _configureList;

        /// <summary>
        /// Creates a new instance of <see cref="WorldStorageContext"/> with the given database options and components.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        /// <param name="models">The model types to initialize.</param>
        public WorldStorageContext(DbContextOptions options, IEnumerable<Type> models) : base(options)
        {
            _configureList = new List<ModelBuilderDelegate>();
            HashSet<Type> componentSet = new HashSet<Type>();
            HashSet<Type> initSet = new HashSet<Type>();

            void AddDepTypes(IEnumerable<Type> depTypes)
            {
                foreach (var depType in depTypes)
                {
                    if (!initSet.Add(depType)) continue;
                    foreach (var method in depType.GetMethods(BindingFlags.Static | BindingFlags.Public |
                                                              BindingFlags.NonPublic))
                        if (method.GetCustomAttributes(typeof(ModelBuilderCallbackAttribute)).Any())
                            _configureList.Add(method.CreateDelegate<ModelBuilderDelegate>());
                }
            }

            foreach (var type in models)
            {
                if (!componentSet.Add(type)) continue;
                foreach (var dep in type.GetCustomAttributes(typeof(StorageDependenciesAttribute)))
                    AddDepTypes(((StorageDependenciesAttribute)dep).Types);
            }

            var baseMethod = typeof(DbContext).GetMethod(nameof(Set), 1, Array.Empty<Type>()) ??
                             throw new ApplicationException();
            var args = Array.Empty<object>();
            foreach (var type in initSet) baseMethod.MakeGenericMethod(type).Invoke(this, args);
        }

        /// <inheritdoc />
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseLazyLoadingProxies();

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder builder)
        {
            foreach (var del in _configureList)
                del(builder);
        }
    }
}

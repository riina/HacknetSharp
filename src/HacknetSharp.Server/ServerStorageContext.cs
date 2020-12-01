using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HacknetSharp.Server.Common;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a database session.
    /// </summary>
    public class ServerStorageContext : DbContext
    {
        /// <summary>
        /// Delegate type for methods with <see cref="ModelBuilderCallbackAttribute"/>
        /// </summary>
        /// <param name="builder">The builder to use.</param>
        public delegate void ModelBuilderDelegate(ModelBuilder builder);

        private readonly List<ModelBuilderDelegate> _configureList;

        /// <summary>
        /// Creates a new instance of <see cref="ServerStorageContext"/> with the given database options and components.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        /// <param name="programs">The program types to initialize.</param>
        /// <param name="models">Additional model types to initialize.</param>
        public ServerStorageContext(DbContextOptions options, IEnumerable<Type> programs, IEnumerable<Type> models) :
            base(options)
        {
            _configureList = new List<ModelBuilderDelegate>();
            HashSet<Type> programSet = new HashSet<Type>();
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

            AddDepTypes(ServerUtil.DefaultModels.Concat(models));

            foreach (var type in programs)
            {
                if (!programSet.Add(type)) continue;
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
            => options.UseLazyLoadingProxies().EnableSensitiveDataLogging();

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder builder)
        {
            foreach (var del in _configureList)
                del(builder);
        }
    }
}

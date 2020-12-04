using System;

namespace hss.Core
{
    /// <summary>
    /// Specifies dependency types for a model type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class StorageDependenciesAttribute : Attribute
    {
        /// <summary>
        /// Types to register.
        /// </summary>
        public Type[] Types;

        /// <summary>
        /// Specifies dependency types for a model type.
        /// </summary>
        /// <param name="types">Types to register.</param>
        public StorageDependenciesAttribute(params Type[] types)
        {
            Types = types;
        }
    }
}

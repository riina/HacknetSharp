using System;

namespace HacknetSharp.Server.Lua
{
    /// <summary>
    /// Indicates a member method should be registered as a global.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method /* | AttributeTargets.Property*/)]
    public class GlobalAttribute : Attribute
    {
        /// <summary>
        /// Indicates a member method should be registered as a global.
        /// </summary>
        /// <param name="delegateType">Delegate type.</param>
        public GlobalAttribute(Type delegateType) => DelegateType = delegateType;

        /// <summary>
        /// Delegate type.
        /// </summary>
        public Type DelegateType { get; set; }

        /// <summary>
        /// Custom name to apply, if any.
        /// </summary>
        public string? Name { get; set; }
    }
}

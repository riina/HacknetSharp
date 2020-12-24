using System;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Indicates that an executable should be ignored when registering types to a world/server.
    /// </summary>
    /// <remarks>
    /// This attribute should be applied to executables that are special and should not be
    /// automatically registered when registering executable types to a world/server.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class IgnoreRegistrationAttribute : Attribute
    {
    }
}

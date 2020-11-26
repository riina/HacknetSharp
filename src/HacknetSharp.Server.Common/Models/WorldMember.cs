using System;

namespace HacknetSharp.Server.Common.Models
{
    public abstract class WorldMember<T> : Model<T> where T : IEquatable<T>
    {
        public virtual WorldModel World { get; set; } = null!;
    }
}

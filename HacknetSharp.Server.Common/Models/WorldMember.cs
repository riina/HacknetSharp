using System;

namespace HacknetSharp.Server.Common.Models
{
    public abstract class WorldMember<T> : Model<T> where T : IEquatable<T>
    {
        public Guid World { get; set; }
    }
}

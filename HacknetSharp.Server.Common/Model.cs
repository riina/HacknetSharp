using System;

namespace HacknetSharp.Server.Common
{
    public abstract class Model<T> where T : IEquatable<T>
    {
        public T Key { get; set; }
    }
}

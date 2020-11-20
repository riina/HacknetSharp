using System;
using System.Threading.Tasks;

namespace HacknetSharp.Server.Common
{
    public abstract class Program
    {
        public abstract Guid Id { get; }
        public abstract Task Invoke(System system);
    }
}

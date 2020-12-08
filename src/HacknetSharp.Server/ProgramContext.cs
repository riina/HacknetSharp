using System;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server
{
    public class ProgramContext : ProcessContext
    {
        public IPersonContext User { get; set; } = null!;
        public ShellProcess Shell { get; set; } = null!;
        public Guid OperationId { get; set; }
        public InvocationType Type { get; set; }
        public int ConWidth { get; set; } = -1;
        public bool IsAI { get; set; }

        public enum InvocationType
        {
            Standard,
            Connect,
            StartUp
        }
    }
}

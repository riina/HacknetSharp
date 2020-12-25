using System;
using System.Collections.Generic;
using System.IO;
using HacknetSharp.Server.Models;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Serialization;

namespace HacknetSharp.Server.Lua
{
    /// <summary>
    /// MoonSharp utilities
    /// </summary>
    /// <remarks>
    /// Based on<br/>
    /// https://github.com/teamRokuro/NetBattle/blob/e273fa58117bcdb6383255ecdebd4a95f1c46d93/NetBattle/ScriptUtil.cs
    /// </remarks>
    public class ScriptUtil
    {
        private static bool _init;

        private static readonly Type[] _defaultTypes =
        {
            // System
            typeof(Guid),
            // HacknetSharp.Server.Lua
            typeof(ScriptManager),
            // HacknetSharp.Server
            typeof(IWorld), typeof(ShellProcess), typeof(ProgramProcess), typeof(ServiceProcess), typeof(Executable),
            typeof(Program), typeof(Service),
            // HacknetSharp.Server.Executable
            typeof(Executable.DelayYieldToken), typeof(Executable.InputYieldToken),
            typeof(Executable.ConfirmYieldToken), typeof(Executable.EditYieldToken),
            // HacknetSharp.Server.Models
            typeof(PersonModel), typeof(MissionModel), typeof(SystemModel), typeof(LoginModel), typeof(FileModel),
            typeof(FileModel.FileKind)
        };

        /// <summary>
        /// Register types for MoonSharp.
        /// </summary>
        /// <param name="types">Types to register.</param>
        public static void MsRegisterTypes(IEnumerable<Type> types)
        {
            foreach (var type in types)
                UserData.RegisterType(type);
        }

        /// <summary>
        /// Dump MoonSharp UserData.
        /// </summary>
        /// <param name="stream">Output stream.</param>
        public static void MsHwDump(Stream stream)
        {
            Init();
            using var writer = new StreamWriter(stream);
            writer.Write(UserData.GetDescriptionOfRegisteredTypes(true).Serialize());
        }

        internal static void Init()
        {
            if (_init) return;
            MsRegisterTypes(_defaultTypes);
            _init = true;
        }
    }
}

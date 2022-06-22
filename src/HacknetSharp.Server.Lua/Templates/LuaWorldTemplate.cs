using System.Collections.Generic;
using System.Linq;
using HacknetSharp.Server.Templates;
using MoonSharp.StaticGlue.Core;

namespace HacknetSharp.Server.Lua.Templates
{
    /// <summary>
    /// Represents a template for a world.
    /// </summary>
    [Scriptable("world_t")]
    public class LuaWorldTemplate : IProxyConversion<WorldTemplate>
    {
        /// <summary>
        /// Label to indicate what template was used.
        /// </summary>
        [Scriptable]
        public string? Label { get; set; }

        /// <summary>
        /// Template for new players.
        /// </summary>
        [Scriptable]
        public string? PlayerSystemTemplate { get; set; }

        /// <summary>
        /// Address range for new players.
        /// </summary>
        [Scriptable]
        public string? PlayerAddressRange { get; set; }

        /// <summary>
        /// Starting mission for new players.
        /// </summary>
        [Scriptable]
        public string? StartingMission { get; set; }

        /// <summary>
        /// Command line for new players.
        /// </summary>
        [Scriptable]
        public string? StartupCommandLine { get; set; }

        /// <summary>
        /// NPC spawners.
        /// </summary>
        public List<LuaPersonGroup>? People { get; set; }

        /// <summary>
        /// Creates and adds a <see cref="LuaPersonGroup"/>.
        /// </summary>
        /// <returns>Object.</returns>
        [Scriptable]
        public LuaPersonGroup CreatePersonGroup()
        {
            LuaPersonGroup group = new();
            (People ??= new List<LuaPersonGroup>()).Add(group);
            return group;
        }

        /// <summary>
        /// Reboot duration in seconds.
        /// </summary>
        [Scriptable]
        public double RebootDuration { get; set; }

        /// <summary>
        /// System disk capacity.
        /// </summary>
        [Scriptable]
        public int DiskCapacity { get; set; }

        /// <summary>
        /// System memory (bytes).
        /// </summary>
        [Scriptable]
        public long SystemMemory { get; set; }

        /// <summary>
        /// Default constructor for deserialization only.
        /// </summary>
        [Scriptable]
        public LuaWorldTemplate()
        {
        }

        /// <summary>
        /// Generates target template.
        /// </summary>
        /// <returns>Target template.</returns>
        [Scriptable]
        public WorldTemplate Generate() =>
            new()
            {
                Label = Label,
                PlayerSystemTemplate = PlayerSystemTemplate,
                StartingMission = StartingMission,
                StartupCommandLine = StartupCommandLine,
                People = People?.Select(v => v.Generate()).ToList(),
                RebootDuration = RebootDuration,
                DiskCapacity = DiskCapacity,
                SystemMemory = SystemMemory
            };

        /// <summary>
        /// Represents a group of 1 or more NPCs or networks to generate.
        /// </summary>
        public class LuaPersonGroup
        {
            /// <summary>
            /// Number of people to generate with this configuration.
            /// </summary>
            [Scriptable]
            public int Count { get; set; }

            /// <summary>
            /// Template for this group.
            /// </summary>
            [Scriptable]
            public string? Template { get; set; }

            /// <summary>
            /// Address range for this group.
            /// </summary>
            [Scriptable]
            public string? AddressRange { get; set; }

            internal WorldTemplate.PersonGroup Generate() => new() { Count = Count, Template = Template, AddressRange = AddressRange };
        }
    }
}

using System;
using System.Collections.Generic;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents an instance of a world in a server.
    /// </summary>
    public class WorldModel : Model<Guid>
    {
        /// <summary>
        /// Name of the world.
        /// </summary>
        public virtual string Name { get; set; } = null!;

        /// <summary>
        /// Label for the server.
        /// </summary>
        public virtual string Label { get; set; } = null!;

        /// <summary>
        /// Template for new players.
        /// </summary>
        public virtual string PlayerSystemTemplate { get; set; } = null!;

        /// <summary>
        /// Starting mission for new players.
        /// </summary>
        public virtual string? StartingMission { get; set; }

        /// <summary>
        /// Command line for new players.
        /// </summary>
        public virtual string? StartupCommandLine { get; set; } = null!;

        /// <summary>
        /// Address range for new players.
        /// </summary>
        public virtual string PlayerAddressRange { get; set; } = null!;

        /// <summary>
        /// Active identities, both users and NPCs.
        /// </summary>
        public virtual HashSet<PersonModel> Persons { get; set; } = null!;

        /// <summary>
        /// Active systems.
        /// </summary>
        public virtual HashSet<SystemModel> Systems { get; set; } = null!;

        /// <summary>
        /// Default reboot duration in seconds.
        /// </summary>
        public virtual double RebootDuration { get; set; }

        /// <summary>
        /// System disk capacity.
        /// </summary>
        public virtual int DiskCapacity { get; set; }

        /// <summary>
        /// System memory (bytes).
        /// </summary>
        public virtual long SystemMemory { get; set; }

        /// <summary>
        /// Current world time.
        /// </summary>
        public virtual double Now { get; set; }

        /// <summary>
        /// Addressed systems in world.
        /// </summary>
        public Dictionary<uint, SystemModel> AddressedSystems { get; set; } = new();

        /// <summary>
        /// Tagged systems in world.
        /// </summary>
        public Dictionary<string, List<SystemModel>> TaggedSystems { get; set; } = new();

        /// <summary>
        /// Systems with non-<see cref="Guid.Empty"/> spawn groups.
        /// </summary>
        public Dictionary<Guid, List<SystemModel>> SpawnGroupSystems { get; set; } = new();

        /// <summary>
        /// Addressed systems in world.
        /// </summary>
        public HashSet<MissionModel> ActiveMissions { get; set; } = new();

        /// <summary>
        /// Tagged systems in world.
        /// </summary>
        public Dictionary<string, List<PersonModel>> TaggedPersons { get; set; } = new();

        /// <summary>
        /// Systems with non-<see cref="Guid.Empty"/> spawn groups.
        /// </summary>
        public Dictionary<Guid, List<PersonModel>> SpawnGroupPersons { get; set; } = new();
    }
}

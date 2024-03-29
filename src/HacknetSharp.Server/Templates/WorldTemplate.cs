﻿using System;
using System.Collections.Generic;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.Templates
{
    /// <summary>
    /// Represents a template for a world.
    /// </summary>
    public class WorldTemplate
    {
        /// <summary>
        /// Label to indicate what template was used.
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Template for new players.
        /// </summary>
        public string? PlayerSystemTemplate { get; set; }

        /// <summary>
        /// Address range for new players.
        /// </summary>
        public string? PlayerAddressRange { get; set; }

        /// <summary>
        /// Starting mission for new players.
        /// </summary>
        public string? StartingMission { get; set; }

        /// <summary>
        /// Command line for new players.
        /// </summary>
        public string? StartupCommandLine { get; set; }

        /// <summary>
        /// NPC spawners.
        /// </summary>
        public List<PersonGroup>? People { get; set; }

        /// <summary>
        /// Creates and adds a <see cref="PersonGroup"/>.
        /// </summary>
        /// <returns>Object.</returns>
        public PersonGroup CreatePersonGroup()
        {
            PersonGroup group = new();
            (People ??= new List<PersonGroup>()).Add(group);
            return group;
        }

        /// <summary>
        /// Reboot duration in seconds.
        /// </summary>
        public double RebootDuration { get; set; }

        /// <summary>
        /// System disk capacity.
        /// </summary>
        public int DiskCapacity { get; set; }

        /// <summary>
        /// System memory (bytes).
        /// </summary>
        public long SystemMemory { get; set; }

        /// <summary>
        /// Default constructor for deserialization only.
        /// </summary>
        public WorldTemplate()
        {
        }

        /// <summary>
        /// Creates a new instance of <see cref="WorldTemplate"/>.
        /// </summary>
        /// <param name="playerSystemTemplate">Player system template.</param>
        public WorldTemplate(string playerSystemTemplate)
        {
            PlayerSystemTemplate = playerSystemTemplate;
            People = new List<PersonGroup>();
        }

        /// <summary>
        /// Apply this template to a world.
        /// </summary>
        /// <param name="database">Database for the world.</param>
        /// <param name="templates">Template group for spawning.</param>
        /// <param name="world">Model to apply to.</param>
        /// <exception cref="KeyNotFoundException">Thrown when a template is missing.</exception>
        /// <exception cref="InvalidOperationException">Thrown when there are missing elements.</exception>
        /// <exception cref="ApplicationException">Thrown when failed to parse template contents.</exception>
        public virtual void ApplyTemplate(IServerDatabase database, TemplateGroup templates, WorldModel world)
        {
            world.PlayerSystemTemplate = PlayerSystemTemplate ??
                                         throw new InvalidOperationException(
                                             $"{nameof(PlayerSystemTemplate)} is null.");
            world.StartingMission = StartingMission;
            world.StartupCommandLine = StartupCommandLine;
            world.Label = Label ?? "Unlabeled Template";
            world.PlayerAddressRange = PlayerAddressRange ?? Constants.DefaultAddressRange;
            world.RebootDuration = RebootDuration;
            world.DiskCapacity = DiskCapacity;
            world.SystemMemory = SystemMemory;
            var worldSpawn = new WorldSpawn(database, world);
            if (People == null) return;
            foreach (var generator in People)
                if (!templates.PersonTemplates.TryGetValue(generator.Template ??
                                                           throw new InvalidOperationException(
                                                               $"Null {nameof(PersonGroup.Template)}"),
                        out var template))
                    throw new KeyNotFoundException($"Unknown template {generator.Template}");
                else
                {
                    int count = Math.Max(1, generator.Count);
                    for (int i = 0; i < count; i++)
                        template.Generate(worldSpawn, templates, generator.AddressRange);
                }
        }

        /// <summary>
        /// Represents a group of 1 or more NPCs or networks to generate.
        /// </summary>
        public class PersonGroup
        {
            /// <summary>
            /// Number of people to generate with this configuration.
            /// </summary>
            public int Count { get; set; }

            /// <summary>
            /// Template for this group.
            /// </summary>
            public string? Template { get; set; }

            /// <summary>
            /// Address range for this group.
            /// </summary>
            public string? AddressRange { get; set; }
        }
    }
}

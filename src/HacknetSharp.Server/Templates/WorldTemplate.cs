using System;
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
        /// Command line for new players.
        /// </summary>
        public string? StartupCommandLine { get; set; }

        /// <summary>
        /// NPC spawners.
        /// </summary>
        public List<PersonGroup>? People { get; set; }

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
            world.Label = Label ?? throw new InvalidOperationException($"{nameof(Label)} is null.");
            world.PlayerSystemTemplate =
                PlayerSystemTemplate ?? throw new InvalidOperationException($"{nameof(PlayerSystemTemplate)} is null.");
            world.StartupCommandLine = StartupCommandLine ??
                                       throw new InvalidOperationException($"{nameof(StartupCommandLine)} is null.");
            world.PlayerAddressRange = PlayerAddressRange ?? Constants.DefaultAddressRange;
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
    }
}

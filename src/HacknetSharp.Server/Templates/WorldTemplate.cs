using System;
using System.Collections.Generic;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.Templates
{
    public class WorldTemplate
    {
        public string? Label { get; set; }
        public string? PlayerSystemTemplate { get; set; }
        public string? PlayerAddressRange { get; set; }
        public string? StartupCommandLine { get; set; }
        public List<PersonGroup>? People { get; set; }

        public class PersonGroup
        {
            public int Count { get; set; }
            public string? Template { get; set; }
            public string? AddressRange { get; set; }
        }

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

using System;
using System.Collections.Generic;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common.Templates
{
    public class WorldTemplate
    {
        public string? Label { get; set; }
        public string? PlayerSystemTemplate { get; set; }
        public string? PlayerAddressRange { get; set; }
        public string? StartupCommandLine { get; set; }
        public List<Generator>? Generators { get; set; }

        public class Generator
        {
            public int Count { get; set; }
            public string? PersonTemplate { get; set; }
            public string? AddressRange { get; set; }
        }

        public void ApplyTemplate(IServerDatabase database, ISpawn spawn, TemplateGroup templates, WorldModel world)
        {
            world.Label = Label ?? throw new InvalidOperationException($"{nameof(Label)} is null.");
            world.PlayerSystemTemplate =
                PlayerSystemTemplate ?? throw new InvalidOperationException($"{nameof(PlayerSystemTemplate)} is null.");
            world.StartupCommandLine = StartupCommandLine ??
                                       throw new InvalidOperationException($"{nameof(StartupCommandLine)} is null.");
            world.PlayerAddressRange = PlayerAddressRange ?? Constants.DefaultAddressRange;
            if (Generators == null) return;
            foreach (var generator in Generators)
                if (!templates.PersonTemplates.TryGetValue(generator.PersonTemplate ??
                                                           throw new InvalidOperationException(
                                                               $"Null {nameof(Generator.PersonTemplate)}"),
                    out var template))
                    throw new KeyNotFoundException($"Unknown template {generator.PersonTemplate}");
                else
                    for (int i = 0; i < generator.Count; i++)
                        template.Generate(database, spawn, templates, world, generator.AddressRange);
        }
    }
}

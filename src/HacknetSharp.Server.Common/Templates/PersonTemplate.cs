using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common.Templates
{
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class PersonTemplate
    {
        public List<string> Usernames { get; set; } = new List<string>();
        public List<string> Passwords { get; set; } = new List<string>();
        public List<string> EmailProviders { get; set; } = new List<string>();
        public List<string> SystemTemplates { get; set; } = new List<string>();
        public int FleetMin { get; set; }
        public int FleetMax { get; set; }
        public List<string> FleetSystemTemplates { get; set; } = new List<string>();

        [ThreadStatic] private static Random? _random;

        private static Random Random => _random ??= new Random();

        public void Generate(ISpawn spawn, TemplateGroup templates, WorldModel world)
        {
            if (Usernames.Count == 0) throw new InvalidOperationException($"{nameof(Usernames)} is empty.");
            if (Passwords.Count == 0) throw new InvalidOperationException($"{nameof(Passwords)} is empty.");
            if (SystemTemplates.Count == 0) throw new InvalidOperationException($"{nameof(SystemTemplates)} is empty.");
            if (FleetMin != 0 && FleetSystemTemplates.Count == 0)
                throw new InvalidOperationException($"{nameof(FleetSystemTemplates)} is empty.");
            string systemTemplateName = SystemTemplates[Random.Next() % SystemTemplates.Count].ToLowerInvariant();
            if (!templates.SystemTemplates.TryGetValue(systemTemplateName, out var systemTemplate))
                throw new KeyNotFoundException($"Unknown template {systemTemplateName}");
            string username = Usernames[Random.Next() % Usernames.Count];
            string password = Passwords[Random.Next() % Passwords.Count];

            var person = spawn.Person(world, username, username);
            var (hash, salt) = CommonUtil.HashPassword(password);
            var system = spawn.System(world, systemTemplate, person, hash, salt);
            person.CurrentSystem = system;
            person.DefaultSystem = system;
            int count = Random.Next(FleetMin, FleetMax + 1);
            for (int i = 0; i < count; i++)
            {
                string fleetSystemTemplateName = FleetSystemTemplates[Random.Next() % FleetSystemTemplates.Count].ToLowerInvariant();
                if (!templates.SystemTemplates.TryGetValue(fleetSystemTemplateName, out var fleetSystemTemplate))
                    throw new KeyNotFoundException($"Unknown template {fleetSystemTemplateName}");
                spawn.System(world, fleetSystemTemplate, person, hash, salt);
            }
        }
    }
}

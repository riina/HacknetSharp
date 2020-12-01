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
        public string? AddressRange { get; set; }
        public List<string> EmailProviders { get; set; } = new List<string>();
        public List<string> SystemTemplates { get; set; } = new List<string>();
        public int FleetMin { get; set; }
        public int FleetMax { get; set; }
        public List<string> FleetSystemTemplates { get; set; } = new List<string>();

        [ThreadStatic] private static Random? _random;

        private static Random Random => _random ??= new Random();

        public void Generate(IServerDatabase database, ISpawn spawn, TemplateGroup templates, WorldModel world,
            string? addressRange)
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

            var range = new IPAddressRange(addressRange ?? AddressRange ?? Constants.DefaultAddressRange);
            var person = spawn.Person(database, world, username, username);
            var (hash, salt) = CommonUtil.HashPassword(password);
            var system = spawn.System(database, world, systemTemplate, person, hash, salt, range);
            person.CurrentSystem = system.Key;
            person.DefaultSystem = system.Key;
            int count = Random.Next(FleetMin, FleetMax + 1);
            for (int i = 0; i < count; i++)
            {
                string fleetSystemTemplateName =
                    FleetSystemTemplates[Random.Next() % FleetSystemTemplates.Count].ToLowerInvariant();
                if (!templates.SystemTemplates.TryGetValue(fleetSystemTemplateName, out var fleetSystemTemplate))
                    throw new KeyNotFoundException($"Unknown template {fleetSystemTemplateName}");
                spawn.System(database, world, fleetSystemTemplate, person, hash, salt, range);
            }
        }
    }
}

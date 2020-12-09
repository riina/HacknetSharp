using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.Templates
{
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class PersonTemplate
    {
        public List<string>? Usernames { get; set; }
        public List<string>? Passwords { get; set; }
        public string? AddressRange { get; set; }
        public List<string>? EmailProviders { get; set; }
        public List<string>? PrimarySystemTemplates { get; set; }
        public int FleetMin { get; set; }
        public int FleetMax { get; set; }
        public List<string>? FleetSystemTemplates { get; set; }

        public List<NetworkEntry>? Network { get; set; }

        public class NetworkEntry
        {
            public string? Template { get; set; } = null!;

            public string? Address { get; set; } = null!;

            public Dictionary<string, string>? Configuration { get; set; }

            public List<string>? Links { get; set; }
        }

        [ThreadStatic] private static Random? _random;

        private static Random Random => _random ??= new Random();

        public virtual void Generate(IServerDatabase database, WorldSpawn spawn, TemplateGroup templates,
            WorldModel world,
            string? addressRange)
        {
            if (Usernames == null) throw new InvalidOperationException($"{nameof(Usernames)} is null.");
            if (Passwords == null) throw new InvalidOperationException($"{nameof(Passwords)} is null.");
            if (PrimarySystemTemplates == null)
                throw new InvalidOperationException($"{nameof(PrimarySystemTemplates)} is null.");
            if (Usernames.Count == 0) throw new InvalidOperationException($"{nameof(Usernames)} is empty.");
            if (Passwords.Count == 0) throw new InvalidOperationException($"{nameof(Passwords)} is empty.");
            if (PrimarySystemTemplates.Count == 0)
                throw new InvalidOperationException($"{nameof(PrimarySystemTemplates)} is empty.");
            if (FleetMin != 0 && (FleetSystemTemplates?.Count ?? 0) == 0)
                throw new InvalidOperationException($"{nameof(FleetSystemTemplates)} is empty.");
            string systemTemplateName = PrimarySystemTemplates[Random.Next() % PrimarySystemTemplates.Count];
            if (!templates.SystemTemplates.TryGetValue(systemTemplateName, out var systemTemplate))
                throw new KeyNotFoundException($"Unknown template {systemTemplateName}");
            string username = Usernames[Random.Next() % Usernames.Count];
            string password = Passwords[Random.Next() % Passwords.Count];

            var range = new IPAddressRange(addressRange ??
                                           AddressRange ?? systemTemplate.AddressRange ??
                                           Constants.DefaultAddressRange);
            var person = spawn.Person(username, username);
            var (hash, salt) = ServerUtil.HashPassword(password);
            var system = spawn.System(systemTemplate, person, hash, salt, range);
            var systems = new List<SystemModel> {system};
            person.DefaultSystem = system.Key;
            if (FleetSystemTemplates != null)
            {
                int count = Random.Next(FleetMin, FleetMax + 1);
                bool fixedRange = addressRange != null || AddressRange != null;
                for (int i = 0; i < count; i++)
                {
                    string fleetSystemTemplateName =
                        FleetSystemTemplates[Random.Next() % FleetSystemTemplates.Count];
                    if (!templates.SystemTemplates.TryGetValue(fleetSystemTemplateName, out var fleetSystemTemplate))
                        throw new KeyNotFoundException($"Unknown template {fleetSystemTemplateName}");
                    systems.Add(spawn.System(fleetSystemTemplate, person, hash, salt,
                        fixedRange
                            ? range
                            : new IPAddressRange(fleetSystemTemplate.AddressRange ?? Constants.DefaultAddressRange)));
                }

                for (int i = 0; i < systems.Count; i++)
                for (int j = i + 1; j < systems.Count; j++)
                {
                    spawn.Connection(systems[i], systems[j], true);
                    spawn.Connection(systems[j], systems[i], true);
                }
            }

            if (Network != null)
            {
                var networkDict = new Dictionary<string, SystemModel>();
                var networkLinkDict = new Dictionary<string, List<string>>();
                foreach (var networkEntry in Network)
                {
                    string address = networkEntry.Address ??
                                     throw new InvalidOperationException(
                                         $"{nameof(NetworkEntry)} missing {nameof(NetworkEntry.Address)}");
                    var addr = new IPAddressRange(address);
                    string template = networkEntry.Template ??
                                      throw new InvalidOperationException(
                                          $"{nameof(NetworkEntry)} missing {nameof(NetworkEntry.Template)}");
                    if (!templates.SystemTemplates.TryGetValue(template, out var netTemplate))
                        throw new KeyNotFoundException($"Unknown template {template}");
                    var netTemplateShim =
                        new SystemTemplateShim(netTemplate) {Configuration = networkEntry.Configuration};
                    var netSystem = spawn.System(netTemplateShim, person, hash, salt, addr);
                    networkDict.Add(address, netSystem);
                    if (networkEntry.Links != null)
                        networkLinkDict.Add(address, networkEntry.Links);
                }

                foreach (var (srcAddr, networkLinks) in networkLinkDict)
                {
                    var baseSystem = networkDict[srcAddr];
                    foreach (var linked in networkLinks) spawn.Connection(baseSystem, networkDict[linked], true);
                }
            }
        }

        private class SystemTemplateShim : SystemTemplate
        {
            private readonly SystemTemplate _baseTemplate;
            public Dictionary<string, string>? Configuration { get; set; }

            public SystemTemplateShim(SystemTemplate baseTemplate)
            {
                _baseTemplate = baseTemplate;
            }

            public override void ApplyTemplate(WorldSpawn spawn, SystemModel model, PersonModel owner, byte[] hash,
                byte[] salt, Dictionary<string, string>? configuration = null)
            {
                if (configuration != null)
                {
                    if (Configuration != null)
                    {
                        var temp = new Dictionary<string, string>(Configuration);
                        foreach (var (key, value) in configuration)
                            temp[key] = value;
                        configuration = temp;
                    }
                }
                else
                    configuration = Configuration;

                _baseTemplate.ApplyTemplate(spawn, model, owner, hash, salt, configuration);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.Templates
{
    /// <summary>
    /// Represents a person template.
    /// </summary>
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class PersonTemplate
    {
        /// <summary>
        /// Fixed username to use.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// Fixed password to use.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Fixed email provider to use.
        /// </summary>
        public string? EmailProvider { get; set; }

        /// <summary>
        /// Fixed primary system template to use.
        /// </summary>
        public string? PrimaryTemplate { get; set; }

        /// <summary>
        /// Fixed primary address to use.
        /// </summary>
        public string? PrimaryAddress { get; set; }

        /// <summary>
        /// Username pool (weighted).
        /// </summary>
        public Dictionary<string, float>? Usernames { get; set; }

        /// <summary>
        /// Adds a username with the specified value and weight.
        /// </summary>
        /// <param name="username">Value.</param>
        /// <param name="weight">Weight.</param>
        public void AddUsername(string username, float weight) => (Usernames ??= new Dictionary<string, float>())[username] = weight;

        /// <summary>
        /// Password pool (weighted).
        /// </summary>
        public Dictionary<string, float>? Passwords { get; set; }

        /// <summary>
        /// Adds a password with the specified value and weight.
        /// </summary>
        /// <param name="password">Value.</param>
        /// <param name="weight">Weight.</param>
        public void AddPassword(string password, float weight) => (Passwords ??= new Dictionary<string, float>())[password] = weight;

        /// <summary>
        /// CIDR range string for address pool.
        /// </summary>
        public string? AddressRange { get; set; }

        /// <summary>
        /// Email provider pool (weighted).
        /// </summary>
        public Dictionary<string, float>? EmailProviders { get; set; }

        /// <summary>
        /// Adds an email provider with the specified value and weight.
        /// </summary>
        /// <param name="emailProvider">Value.</param>
        /// <param name="weight">Weight.</param>
        public void AddEmailProvider(string emailProvider, float weight) => (EmailProviders ??= new Dictionary<string, float>())[emailProvider] = weight;

        /// <summary>
        /// Primary template pool (weighted).
        /// </summary>
        public Dictionary<string, float>? PrimaryTemplates { get; set; }

        /// <summary>
        /// Adds a primary template with the specified value and weight.
        /// </summary>
        /// <param name="primaryTemplate">Value.</param>
        /// <param name="weight">Weight.</param>
        public void AddPrimaryTemplate(string primaryTemplate, float weight) => (PrimaryTemplates ??= new Dictionary<string, float>())[primaryTemplate] = weight;

        /// <summary>
        /// Minimum # in generated fleet.
        /// </summary>
        public int FleetMin { get; set; }

        /// <summary>
        /// Maximum # in generated fleet.
        /// </summary>
        public int FleetMax { get; set; }

        /// <summary>
        /// Fleet template pool (weighted).
        /// </summary>
        public Dictionary<string, float>? FleetTemplates { get; set; }

        /// <summary>
        /// Adds a fleet template with the specified value and weight.
        /// </summary>
        /// <param name="fleetTemplate">Value.</param>
        /// <param name="weight">Weight.</param>
        public void AddFleetTemplate(string fleetTemplate, float weight) => (FleetTemplates ??= new Dictionary<string, float>())[fleetTemplate] = weight;

        /// <summary>
        /// Fixed-system network to generate.
        /// </summary>
        public List<NetworkEntry>? Network { get; set; }

        /// <summary>
        /// Creates and adds a <see cref="NetworkEntry"/>.
        /// </summary>
        /// <returns>Object.</returns>
        public NetworkEntry CreateNetworkEntry()
        {
            NetworkEntry networkEntry = new();
            (Network ??= new List<NetworkEntry>()).Add(networkEntry);
            return networkEntry;
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
        /// CPU cycles required to crack proxy.
        /// </summary>
        public double ProxyClocks { get; set; }

        /// <summary>
        /// Proxy cracking speed.
        /// </summary>
        public double ClockSpeed { get; set; }

        /// <summary>
        /// System memory (bytes).
        /// </summary>
        public long SystemMemory { get; set; }

        /// <summary>
        /// Tag for lookup.
        /// </summary>
        public string? Tag { get; set; }

        [ThreadStatic] private static Random? _random;

        private static Random Random => _random ??= new Random();

        /// <summary>
        /// Default constructor for deserialization only.
        /// </summary>
        public PersonTemplate()
        {
        }

        /// <summary>
        /// Generates a person using this template.
        /// </summary>
        /// <param name="spawn">World spawner instance.</param>
        /// <param name="templates">Template group to use.</param>
        /// <param name="addressRange">Address range to override template with.</param>
        /// <exception cref="InvalidOperationException">Thrown when there is insufficient data to successfully apply template.</exception>
        /// <exception cref="KeyNotFoundException">Thrown when a template is not found.</exception>
        public virtual void Generate(WorldSpawn spawn, TemplateGroup templates, string? addressRange)
        {
            if (Username == null && (Usernames == null || Usernames.Count == 0))
                throw new InvalidOperationException("No usernames available.");
            if (Password == null && (Passwords == null || Passwords.Count == 0))
                throw new InvalidOperationException("No passwords available.");
            if (PrimaryTemplate == null && (PrimaryTemplates == null || PrimaryTemplates.Count == 0))
                throw new InvalidOperationException("No primary system templates.");
            if (FleetMin != 0 && (FleetTemplates?.Count ?? 0) == 0)
                throw new InvalidOperationException($"{nameof(FleetTemplates)} is empty.");
            string systemTemplateName = PrimaryTemplate ?? PrimaryTemplates!.SelectWeighted();
            if (!templates.SystemTemplates.TryGetValue(systemTemplateName, out var systemTemplate))
                throw new KeyNotFoundException($"Unknown template {systemTemplateName}");
            string username = Username ?? Usernames!.SelectWeighted();
            string password = Password ?? Passwords!.SelectWeighted();

            var range = new IPAddressRange(addressRange ??
                                           AddressRange ??
                                           Constants.DefaultAddressRange);
            var person = spawn.Person(username, username);
            person.RebootDuration = RebootDuration;
            person.DiskCapacity = DiskCapacity;
            person.ProxyClocks = ProxyClocks;
            person.ClockSpeed = ClockSpeed;
            person.Tag = Tag;
            var genPassword = ServerUtil.HashPassword(password);

            if (Network != null)
            {
                var networkDict = new Dictionary<string, SystemModel>();
                var networkLinkDict = new Dictionary<string, List<string>>();
                foreach (var networkEntry in Network)
                {
                    string address = networkEntry.Address ??
                                     throw new InvalidOperationException(
                                         $"{nameof(NetworkEntry)} missing {nameof(NetworkEntry.Address)}");
                    var addr = new IPAddressRange(address).OnHost(range);
                    string template = networkEntry.Template ??
                                      throw new InvalidOperationException(
                                          $"{nameof(NetworkEntry)} missing {nameof(NetworkEntry.Template)}");
                    if (!templates.SystemTemplates.TryGetValue(template, out var netTemplate))
                        throw new KeyNotFoundException($"Unknown template {template}");
                    var netTemplateShim =
                        new SystemTemplateShim(netTemplate) { Configuration = networkEntry.Configuration };
                    var netSystem = spawn.System(netTemplateShim, template, person, genPassword, addr);
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

            SystemModel system = PrimaryAddress != null
                ? spawn.System(systemTemplate, systemTemplateName, person, genPassword, new IPAddressRange(PrimaryAddress).OnHost(range))
                : spawn.System(systemTemplate, systemTemplateName, person, genPassword, range);
            person.DefaultSystem = system.Key;

            if (FleetTemplates != null)
            {
                var systems = new List<SystemModel> { system };
                int count = Random.Next(FleetMin, FleetMax + 1);
                bool fixedRange = addressRange != null || AddressRange != null;
                for (int i = 0; i < count; i++)
                {
                    string fleetSystemTemplateName = FleetTemplates.SelectWeighted();
                    if (!templates.SystemTemplates.TryGetValue(fleetSystemTemplateName, out var fleetSystemTemplate))
                        throw new KeyNotFoundException($"Unknown template {fleetSystemTemplateName}");
                    systems.Add(spawn.System(fleetSystemTemplate, fleetSystemTemplateName, person, genPassword, fixedRange
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
        }

        private class SystemTemplateShim : SystemTemplate
        {
            private readonly SystemTemplate _baseTemplate;
            public Dictionary<string, string>? Configuration { get; set; }

            public SystemTemplateShim(SystemTemplate baseTemplate)
            {
                _baseTemplate = baseTemplate;
            }

            public override void ApplyTemplate(WorldSpawn spawn, SystemModel model,
                Dictionary<string, string>? configuration = null)
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

                _baseTemplate.ApplyTemplate(spawn, model, configuration);
            }
        }
    }

    /// <summary>
    /// Represents an entry in a person template's fixed-node network.
    /// </summary>
    public class NetworkEntry
    {
        /// <summary>
        /// Template to use.
        /// </summary>
        public string? Template { get; set; }

        /// <summary>
        /// Specific address for system.
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// Additional replacements to pass to system template.
        /// </summary>
        public Dictionary<string, string>? Configuration { get; set; }

        /// <summary>
        /// Adds a replacement with the specified value and weight.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void AddReplacement(string key, string value) => (Configuration ??= new Dictionary<string, string>())[key] = value;

        /// <summary>
        /// Other <see cref="NetworkEntry.Address"/>es to create local links to.
        /// </summary>
        public List<string>? Links { get; set; }

        /// <summary>
        /// Adds a link.
        /// </summary>
        /// <param name="link">Value.</param>
        public void AddLink(string link) => (Links ??= new List<string>()).Add(link);
    }
}

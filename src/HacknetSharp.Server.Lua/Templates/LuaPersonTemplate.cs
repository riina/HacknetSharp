using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HacknetSharp.Server.Templates;
using MoonSharp.StaticGlue.Core;

namespace HacknetSharp.Server.Lua.Templates
{
    /// <summary>
    /// Represents a person template.
    /// </summary>
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [Scriptable("person_t")]
    public class LuaPersonTemplate : IProxyConversion<PersonTemplate>
    {
        /// <summary>
        /// Fixed username to use.
        /// </summary>
        [Scriptable]
        public string? Username { get; set; }

        /// <summary>
        /// Fixed password to use.
        /// </summary>
        [Scriptable]
        public string? Password { get; set; }

        /// <summary>
        /// Fixed email provider to use.
        /// </summary>
        [Scriptable]
        public string? EmailProvider { get; set; }

        /// <summary>
        /// Fixed primary system template to use.
        /// </summary>
        [Scriptable]
        public string? PrimaryTemplate { get; set; }

        /// <summary>
        /// Fixed primary address to use.
        /// </summary>
        [Scriptable]
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
        [Scriptable]
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
        [Scriptable]
        public void AddPassword(string password, float weight) => (Passwords ??= new Dictionary<string, float>())[password] = weight;

        /// <summary>
        /// CIDR range string for address pool.
        /// </summary>
        [Scriptable]
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
        [Scriptable]
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
        [Scriptable]
        public void AddPrimaryTemplate(string primaryTemplate, float weight) => (PrimaryTemplates ??= new Dictionary<string, float>())[primaryTemplate] = weight;

        /// <summary>
        /// Minimum # in generated fleet.
        /// </summary>
        [Scriptable]
        public int FleetMin { get; set; }

        /// <summary>
        /// Maximum # in generated fleet.
        /// </summary>
        [Scriptable]
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
        [Scriptable]
        public void AddFleetTemplate(string fleetTemplate, float weight) => (FleetTemplates ??= new Dictionary<string, float>())[fleetTemplate] = weight;

        /// <summary>
        /// Fixed-system network to generate.
        /// </summary>
        public List<LuaNetworkEntry>? Network { get; set; }

        /// <summary>
        /// Creates and adds a <see cref="LuaNetworkEntry"/>.
        /// </summary>
        /// <returns>Object.</returns>
        [Scriptable]
        public LuaNetworkEntry CreateNetworkEntry()
        {
            LuaNetworkEntry networkEntry = new();
            (Network ??= new List<LuaNetworkEntry>()).Add(networkEntry);
            return networkEntry;
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
        /// CPU cycles required to crack proxy.
        /// </summary>
        [Scriptable]
        public double ProxyClocks { get; set; }

        /// <summary>
        /// Proxy cracking speed.
        /// </summary>
        [Scriptable]
        public double ClockSpeed { get; set; }

        /// <summary>
        /// System memory (bytes).
        /// </summary>
        [Scriptable]
        public long SystemMemory { get; set; }

        /// <summary>
        /// Tag for lookup.
        /// </summary>
        [Scriptable]
        public string? Tag { get; set; }

        /// <summary>
        /// Default constructor for deserialization only.
        /// </summary>
        [Scriptable]
        public LuaPersonTemplate()
        {
        }

        /// <summary>
        /// Generates target template.
        /// </summary>
        /// <returns>Target template.</returns>
        [Scriptable]
        public PersonTemplate Generate() =>
            new()
            {
                Username = Username,
                Password = Password,
                EmailProvider = EmailProvider,
                PrimaryTemplate = PrimaryTemplate,
                PrimaryAddress = PrimaryAddress,
                Usernames = Usernames,
                Passwords = Passwords,
                AddressRange = AddressRange,
                EmailProviders = EmailProviders,
                PrimaryTemplates = PrimaryTemplates,
                FleetMin = FleetMin,
                FleetMax = FleetMax,
                FleetTemplates = FleetTemplates,
                Network = Network?.Select(v => v.Generate()).ToList(),
                RebootDuration = RebootDuration,
                DiskCapacity = DiskCapacity,
                ProxyClocks = ProxyClocks,
                ClockSpeed = ClockSpeed,
                SystemMemory = SystemMemory,
                Tag = Tag
            };
    }

    /// <summary>
    /// Represents an entry in a person template's fixed-node network.
    /// </summary>
    public class LuaNetworkEntry
    {
        /// <summary>
        /// Template to use.
        /// </summary>
        [Scriptable]
        public string? Template { get; set; }

        /// <summary>
        /// Specific address for system.
        /// </summary>
        [Scriptable]
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
        [Scriptable]
        public void AddReplacement(string key, string value) => (Configuration ??= new Dictionary<string, string>())[key] = value;

        /// <summary>
        /// Other <see cref="LuaNetworkEntry.Address"/>es to create local links to.
        /// </summary>
        public List<string>? Links { get; set; }

        /// <summary>
        /// Adds a link.
        /// </summary>
        /// <param name="link">Value.</param>
        [Scriptable]
        public void AddLink(string link) => (Links ??= new List<string>()).Add(link);

        internal NetworkEntry Generate() => new() { Template = Template, Address = Address, Configuration = Configuration, Links = Links };
    }
}

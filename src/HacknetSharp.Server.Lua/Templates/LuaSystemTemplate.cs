using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HacknetSharp.Server.Templates;
using MoonSharp.StaticGlue.Core;

namespace HacknetSharp.Server.Lua.Templates
{
    /// <summary>
    /// Represents a template for a system.
    /// </summary>
    [Scriptable("system_t")]
    public class LuaSystemTemplate : IProxyConversion<SystemTemplate>
    {
        /// <summary>
        /// System name (with replacement support).
        /// </summary>
        [Scriptable]
        public string? Name { get; set; }

        /// <summary>
        /// Operating system name.
        /// </summary>
        [Scriptable]
        public string? OsName { get; set; }

        /// <summary>
        /// Address range. Overridden by person template's <see cref="LuaPersonTemplate.AddressRange"/> if present.
        /// </summary>
        [Scriptable]
        public string? AddressRange { get; set; }

        /// <summary>
        /// Command to execute on connect.
        /// </summary>
        [Scriptable]
        public string? ConnectCommandLine { get; set; }

        /// <summary>
        /// Vulnerabilities on this system.
        /// </summary>
        public List<LuaVulnerability>? Vulnerabilities { get; set; }

        /// <summary>
        /// Creates and adds a <see cref="LuaVulnerability"/>.
        /// </summary>
        /// <returns>Object.</returns>
        [Scriptable]
        public LuaVulnerability CreateVulnerability()
        {
            LuaVulnerability vulnerability = new();
            (Vulnerabilities ??= new List<LuaVulnerability>()).Add(vulnerability);
            return vulnerability;
        }

        /// <summary>
        /// Minimum number of exploits required to gain entry to the system.
        /// </summary>
        [Scriptable]
        public int RequiredExploits { get; set; }

        /// <summary>
        /// Additional username-to-password pairs (with + postfix on username to indicate admin).
        /// </summary>
        public Dictionary<string, string>? Users { get; set; }

        /// <summary>
        /// Adds a user.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        [Scriptable]
        public void AddUser(string username, string password)
        {
            (Users ??= new Dictionary<string, string>())[username] = password;
        }

        /// <summary>
        /// Base filesystem structure.
        /// </summary>
        /// <remarks>
        /// The keys are usernames with replacement support (Name, UserName).
        /// <br/>
        /// Filesystem entries are formatted as "type[permissions]:path args".
        /// Permissions are just 3 */^/+ for
        /// RWE, *:everyone/^:owner/+:admin can perform that operation.
        /// <br/>
        /// - fold: Folder.
        /// <br/>
        /// - prog: Program, arg[0] is the progCode of the program to execute.
        /// <br/>
        /// - text: Text content, arg[0] is the text to include. Unfortunately
        /// it needs to be wrapped in quotes.
        /// <br/>
        /// - file: Content file, arg[0] determines file path. Not yet
        /// implemented.
        /// <br/>
        /// - blob: Blob file, arg[0] determines file path. Not yet implemented.
        /// </remarks>
        public Dictionary<string, List<string>>? Filesystem { get; set; }

        /// <summary>
        /// Adds files.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="files">Files.</param>
        [Scriptable]
        public void AddFiles(string key, IList files)
        {
            Filesystem ??= new Dictionary<string, List<string>>();
            if (!Filesystem.TryGetValue(key, out var list)) Filesystem[key] = list = new List<string>();
            list.AddRange(files.OfType<string>());
        }

        /// <summary>
        /// Timed tasks.
        /// </summary>
        public List<LuaCron>? Tasks { get; set; }

        /// <summary>
        /// Creates and adds a <see cref="LuaCron"/>.
        /// </summary>
        /// <returns>Object.</returns>
        [Scriptable]
        public LuaCron CreateCron()
        {
            LuaCron cron = new();
            (Tasks ??= new List<LuaCron>()).Add(cron);
            return cron;
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
        /// Number of firewall iterations required for full decode.
        /// </summary>
        [Scriptable]
        public int FirewallIterations { get; set; }

        /// <summary>
        /// Length of firewall analysis string.
        /// </summary>
        [Scriptable]
        public int FirewallLength { get; set; }

        /// <summary>
        /// Additional delay per firewall step.
        /// </summary>
        [Scriptable]
        public double FirewallDelay { get; set; }

        /// <summary>
        /// Fixed firewall string.
        /// </summary>
        [Scriptable]
        public string? FixedFirewall { get; set; }

        /// <summary>
        /// Tag for lookup.
        /// </summary>
        [Scriptable]
        public string? Tag { get; set; }

        /// <summary>
        /// Default constructor for deserialization only.
        /// </summary>
        [Scriptable]
        public LuaSystemTemplate()
        {
        }

        /// <summary>
        /// Generates target template.
        /// </summary>
        /// <returns>Target template.</returns>
        [Scriptable]
        public SystemTemplate Generate() =>
            new()
            {
                Name = Name,
                OsName = OsName,
                AddressRange = AddressRange,
                ConnectCommandLine = ConnectCommandLine,
                Vulnerabilities = Vulnerabilities?.Select(v => v.Generate()).ToList(),
                RequiredExploits = RequiredExploits,
                Users = Users,
                Filesystem = Filesystem,
                Tasks = Tasks?.Select(v => v.Generate()).ToList(),
                RebootDuration = RebootDuration,
                DiskCapacity = DiskCapacity,
                ProxyClocks = ProxyClocks,
                ClockSpeed = ClockSpeed,
                SystemMemory = SystemMemory,
                FirewallIterations = FirewallIterations,
                FirewallLength = FirewallLength,
                FirewallDelay = FirewallDelay,
                FixedFirewall = FixedFirewall,
                Tag = Tag
            };
    }

    /// <summary>
    /// Represents a vulnerability on the system.
    /// </summary>
    public class LuaVulnerability
    {
        /// <summary>
        /// Entrypoint / port (e.g. "22").
        /// </summary>
        [Scriptable]
        public string? EntryPoint { get; set; }

        /// <summary>
        /// Protocol this vulnerability is for (e.g. "ssh").
        /// </summary>
        [Scriptable]
        public string? Protocol { get; set; }

        /// <summary>
        /// Number of exploits this vulnerability represents.
        /// </summary>
        [Scriptable]
        public int Exploits { get; set; }

        /// <summary>
        /// Optional CVE string (trivia).
        /// </summary>
        [Scriptable]
        public string? Cve { get; set; }

        internal Vulnerability Generate() => new() { EntryPoint = EntryPoint, Protocol = Protocol, Exploits = Exploits, Cve = Cve };
    }

    /// <summary>
    /// Represents a timed task.
    /// </summary>
    public class LuaCron
    {
        /// <summary>
        /// Task content.
        /// </summary>
        [Scriptable]
        public string? Content { get; set; }

        /// <summary>
        /// Initial start time.
        /// </summary>
        [Scriptable]
        public double Start { get; set; }

        /// <summary>
        /// Task delay.
        /// </summary>
        [Scriptable]
        public double Delay { get; set; }

        /// <summary>
        /// End time.
        /// </summary>
        [Scriptable]
        public double End { get; set; }

        internal Cron Generate() => new() { Content = Content, Start = Start, Delay = Delay, End = End };
    }
}

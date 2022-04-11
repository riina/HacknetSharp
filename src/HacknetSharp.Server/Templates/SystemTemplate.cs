using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HacknetSharp.Server.Models;
using MoonSharp.StaticGlue.Core;

namespace HacknetSharp.Server.Templates
{
    /// <summary>
    /// Represents a template for a system.
    /// </summary>
    [Scriptable("system_t")]
    public class SystemTemplate
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
        /// Address range. Overridden by person template's <see cref="PersonTemplate.AddressRange"/> if present.
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
        // TODO scripting layer
        public List<Vulnerability>? Vulnerabilities { get; set; }

        /// <summary>
        /// Minimum number of exploits required to gain entry to the system.
        /// </summary>
        [Scriptable]
        public int RequiredExploits { get; set; }

        /// <summary>
        /// Additional username-to-password pairs (with + postfix on username to indicate admin).
        /// </summary>
        // TODO scripting layer
        public Dictionary<string, string>? Users { get; set; }

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
        // TODO scripting layer
        public Dictionary<string, List<string>>? Filesystem { get; set; }

        /// <summary>
        /// Timed tasks.
        /// </summary>
        // TODO scripting layer
        public List<Cron>? Tasks { get; set; }

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

        private static readonly Regex s_userRegex = new(@"([A-Za-z]+)(\+)?");
        private static readonly Regex s_fileRegex = new(@"([A-Za-z0-9]+)([*^+]{3})?:([\S\s]+)");

        /// <summary>
        /// efault constructor for deserialization only.
        /// </summary>
        public SystemTemplate()
        {
        }

        /// <summary>
        /// efault constructor for deserialization only.
        /// </summary>
        /// <param name="name">Format-able system name.</param>
        /// <param name="osName">OS name.</param>
        public SystemTemplate(string name, string osName)
        {
            Name = name;
            OsName = osName;
        }

        /// <summary>
        /// Applies this template to a system.
        /// </summary>
        /// <param name="spawn">World spawner instance.</param>
        /// <param name="model">System model to apply to.</param>
        /// <param name="owner">Owner model.</param>
        /// <param name="hash">Owner's password hash.</param>
        /// <param name="salt">Owner's password salt.</param>
        /// <exception cref="InvalidOperationException">Thrown when there are missing elements.</exception>
        /// <exception cref="ApplicationException">Thrown when failed to parse template contents.</exception>
        public virtual void ApplyOwner(WorldSpawn spawn, SystemModel model, PersonModel owner, byte[] hash,
            byte[] salt)
        {
            spawn.Login(model, owner.UserName, hash, salt, true, owner);
        }

        /// <summary>
        /// Applies this template to a system.
        /// </summary>
        /// <param name="spawn">World spawner instance.</param>
        /// <param name="model">System model to apply to.</param>
        /// <param name="configuration">Additional replacement set.</param>
        /// <exception cref="InvalidOperationException">Thrown when there are missing elements.</exception>
        /// <exception cref="ApplicationException">Thrown when failed to parse template contents.</exception>
        public virtual void ApplyTemplate(WorldSpawn spawn, SystemModel model,
            Dictionary<string, string>? configuration = null)
        {
            var repDict = configuration != null
                ? new Dictionary<string, string>(configuration)
                : new Dictionary<string, string>();
            var owner = model.Owner;
            var ownerLogin = model.Logins.FirstOrDefault(l => l.Person == owner.Key);
            if (ownerLogin == null) throw new InvalidOperationException("Owner has no login");
            repDict["Owner.Name"] = owner.Name;
            repDict["Owner.UserName"] = owner.UserName;
            model.Name = (Name ?? throw new InvalidOperationException($"{nameof(Name)} is null."))
                .ApplyReplacements(repDict);
            model.OsName = OsName ?? throw new InvalidOperationException($"{nameof(OsName)} is null.");
            model.ConnectCommandLine = ConnectCommandLine?.ApplyReplacements(repDict);
            model.RequiredExploits = RequiredExploits;
            model.FirewallIterations = FirewallIterations;
            model.FirewallLength = FirewallLength;
            model.FirewallDelay = FirewallDelay;
            model.FixedFirewall = FixedFirewall;
            model.Tag = Tag;
            if (FixedFirewall != null)
                model.FirewallIterations = FixedFirewall.Length;
            model.RebootDuration =
                RebootDuration > 0 ? RebootDuration :
                owner.RebootDuration > 0 ? owner.RebootDuration : model.World.RebootDuration;
            model.DiskCapacity = DiskCapacity > 0 ? DiskCapacity :
                owner.DiskCapacity > 0 ? owner.DiskCapacity :
                model.World.DiskCapacity > 0 ? model.World.DiskCapacity : ServerConstants.DefaultDiskCapacity;
            model.ProxyClocks = ProxyClocks > 0 ? ProxyClocks :
                owner.ProxyClocks > 0 ? owner.ProxyClocks : 0;
            model.ClockSpeed = ClockSpeed > 0 ? ClockSpeed :
                owner.ClockSpeed > 0 ? owner.ClockSpeed : ServerConstants.DefaultClockSpeed;
            model.SystemMemory = SystemMemory > 0 ? SystemMemory :
                owner.SystemMemory > 0 ? owner.SystemMemory :
                model.World.SystemMemory > 0 ? model.World.SystemMemory : ServerConstants.DefaultSystemMemory;
            var unameToLoginDict = new Dictionary<string, LoginModel> { [ownerLogin.User] = ownerLogin };
            foreach (var login in model.Logins.Where(l => l != ownerLogin).ToList()) spawn.RemoveLogin(login);
            if (Users != null)
                foreach (var userKvp in Users)
                {
                    var match = s_userRegex.Match(userKvp.Key);
                    if (!match.Success) throw new ApplicationException($"Failed to parse user for {userKvp.Key}");
                    var (hashSub, saltSub) = ServerUtil.HashPassword(userKvp.Value);
                    string uname = match.Groups[1].Value;
                    unameToLoginDict.Add(uname, spawn.Login(model, uname.ToLowerInvariant(), hashSub,
                        saltSub, match.Groups[2].Success));
                }

            if (Vulnerabilities != null)
                foreach (var vuln in Vulnerabilities)
                {
                    if (vuln.EntryPoint == null)
                        throw new InvalidOperationException(
                            $"Vulnerability does not have {nameof(Vulnerability.EntryPoint)}");
                    if (vuln.Protocol == null)
                        throw new InvalidOperationException(
                            $"Vulnerability does not have {nameof(Vulnerability.Protocol)}");
                    spawn.Vulnerability(model, vuln.Protocol, vuln.EntryPoint, vuln.Exploits, vuln.Cve);
                }

            foreach (var task in model.Tasks.ToList()) spawn.RemoveCron(task);
            if (Tasks != null)
                foreach (var task in Tasks)
                {
                    if (task.Content == null)
                        throw new InvalidOperationException($"Task does not have {nameof(Cron.Content)}");
                    spawn.Cron(model, task.Content, task.Start + model.World.Now, task.Delay, task.End);
                }

            foreach (var file in model.Files) spawn.RemoveFileRaw(file);
            if (Filesystem != null)
                foreach (var kvp in Filesystem)
                {
                    string uname = kvp.Key.ApplyReplacements(repDict);
                    if (!unameToLoginDict.TryGetValue(uname, out var fsLogin))
                        throw new ApplicationException($"No login for uname {uname}");
                    repDict["Name"] = uname;
                    repDict["UserName"] = uname;
                    foreach (var file in kvp.Value)
                    {
                        var match = s_fileRegex.Match(file);
                        if (!match.Success) throw new ApplicationException($"Failed to parse file entry for {match}");
                        var args = match.Groups[3].Value.SplitCommandLine();
                        if (args.Length == 0)
                            throw new ApplicationException($"Not enough arguments to file entry {file}");
                        string mainPath = Executable.GetNormalized(args[0]).ApplyReplacements(repDict);
                        string path = Executable.GetDirectoryName(mainPath) ??
                                      throw new ApplicationException($"Path cannot be {mainPath}");
                        string name = Executable.GetFileName(mainPath);
                        FileModel fileModel;
                        string content = new StringBuilder().AppendJoin(' ', args.Skip(1)).ToString()
                            .ApplyReplacements(repDict);
                        switch (match.Groups[1].Value.ToLowerInvariant())
                        {
                            case "fold":
                                fileModel = spawn.Folder(model, fsLogin, name, path);
                                break;
                            case "prog":
                                if (args.Length < 2)
                                    throw new ApplicationException($"Not enough arguments to file entry {file}");
                                fileModel = spawn.ProgFile(model, fsLogin, name, path, content);
                                break;
                            case "text":
                                if (args.Length < 2)
                                    throw new ApplicationException($"Not enough arguments to file entry {file}");
                                fileModel = spawn.TextFile(model, fsLogin, name, path, content);
                                break;
                            case "file":
                                if (args.Length < 2)
                                    throw new ApplicationException($"Not enough arguments to file entry {file}");
                                fileModel = spawn.FileFile(model, fsLogin, name, path, content);
                                break;
                            case "blob":
                                throw new NotImplementedException();
                            default:
                                throw new ApplicationException($"Unknown file model type in file entry {file}");
                        }

                        if (match.Groups[2].Success)
                        {
                            string matchStr = match.Groups[2].Value;
                            fileModel.Read = CharToAccessLevel(matchStr[0]);
                            fileModel.Write = CharToAccessLevel(matchStr[1]);
                            fileModel.Execute = CharToAccessLevel(matchStr[2]);
                        }
                    }

                    repDict.Remove("Name");
                    repDict.Remove("UserName");
                }
        }

        private static FileModel.AccessLevel CharToAccessLevel(char c) => c switch
        {
            '*' => FileModel.AccessLevel.Everyone,
            '^' => FileModel.AccessLevel.Owner,
            '+' => FileModel.AccessLevel.Admin,
            _ => throw new ApplicationException($"Unknown access level character {c}")
        };
    }

    /// <summary>
    /// Represents a vulnerability on the system.
    /// </summary>
    public class Vulnerability
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
    }

    /// <summary>
    /// Represents a timed task.
    /// </summary>
    public class Cron
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
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a world-level spawn/despawn manager.
    /// </summary>
    public class WorldSpawn
    {
        /// <summary>
        /// Server database this instance is backed by.
        /// </summary>
        protected IServerDatabase Database { get; }

        /// <summary>
        /// World this spawner belongs to.
        /// </summary>
        protected WorldModel World { get; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldSpawn"/>.
        /// </summary>
        /// <param name="database">Database to work on.</param>
        /// <param name="world">World model to work on.</param>
        public WorldSpawn(IServerDatabase database, WorldModel world)
        {
            Database = database;
            World = world;
        }

        /// <summary>
        /// Creates a new person.
        /// </summary>
        /// <param name="name">Proper name.</param>
        /// <param name="userName">Username.</param>
        /// <param name="user">User model if associated with a user.</param>
        /// <returns>Generated model.</returns>
        public PersonModel Person(string name, string userName, UserModel? user = null)
        {
            var person = new PersonModel
            {
                Key = Guid.NewGuid(),
                World = World,
                Name = name,
                UserName = userName,
                Systems = new HashSet<SystemModel>(),
                User = user
            };
            user?.Identities.Add(person);
            World.Persons.Add(person);
            Database.Add(person);
            return person;
        }

        private readonly Random _random = new();

        /// <summary>
        /// Creates a new system.
        /// </summary>
        /// <param name="template">System template.</param>
        /// <param name="owner">System owner.</param>
        /// <param name="hash">Owner password hash.</param>
        /// <param name="salt">Owner password salt.</param>
        /// <param name="range">Address range or single address.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="ApplicationException">Thrown when address range is not IPv4 range.</exception>
        public unsafe SystemModel System(SystemTemplate template, PersonModel owner, byte[] hash, byte[] salt,
            IPAddressRange range)
        {
            if (!range.TryGetIPv4HostAndSubnetMask(out uint host, out uint subnetMask))
                throw new ApplicationException("Address range is not IPv4");
            uint resAddr;
            var systems = World.Systems.Where(s => ((s.Address & subnetMask) ^ host) == 0).Select(s => s.Address)
                .ToHashSet();
            if (range.PrefixBits < 32)
            {
                if (systems.Count >= 1 << (32 - range.PrefixBits))
                    throw new ApplicationException("Specified range has been saturated");

                uint invSubnetMask = ~subnetMask;
                uint gen;
                Span<byte> span = new((byte*)&gen, 4);
                int i = -1;
                do _random.NextBytes(span);
                while (++i < 10 && systems.Contains(gen & invSubnetMask));
                if (i == 10)
                    for (uint j = 0; j <= invSubnetMask && systems.Contains(gen); j++)
                        gen = j;
                resAddr = (gen & invSubnetMask) ^ host;
            }
            else
            {
                if (systems.Count >= 1)
                    throw new ApplicationException($"System with target address {range} already exists");
                resAddr = host;
            }

            return System(template, owner, hash, salt, resAddr);
        }

        /// <summary>
        /// Creates a new system.
        /// </summary>
        /// <param name="template">System template.</param>
        /// <param name="owner">System owner.</param>
        /// <param name="hash">Owner password hash.</param>
        /// <param name="salt">Owner password salt.</param>
        /// <param name="address">Address.</param>
        /// <returns>Generated model.</returns>
        public SystemModel System(SystemTemplate template, PersonModel owner, byte[] hash, byte[] salt, uint address)
        {
            var system = new SystemModel
            {
                Address = address,
                Key = Guid.NewGuid(),
                World = World,
                Owner = owner,
                Files = new HashSet<FileModel>(),
                Logins = new HashSet<LoginModel>(),
                KnownSystems = new HashSet<KnownSystemModel>(),
                KnowingSystems = new HashSet<KnownSystemModel>(),
                Vulnerabilities = new HashSet<VulnerabilityModel>(),
                BootTime = World.Now
            };
            template.ApplyTemplate(this, system, owner, hash, salt);
            owner.Systems.Add(system);
            World.Systems.Add(system);
            Database.Add(system);
            return system;
        }

        /// <summary>
        /// Creates a new A-to-B system knowledge connection.
        /// </summary>
        /// <param name="from">System that knows the other.</param>
        /// <param name="to">System that is known by the other.</param>
        /// <param name="local">If true, treat as knowing the other system as a local status-queryable system.</param>
        /// <returns>Generated model.</returns>
        public KnownSystemModel Connection(SystemModel from, SystemModel to, bool local)
        {
            var c = new KnownSystemModel
            {
                From = from,
                FromKey = from.Key,
                To = to,
                ToKey = to.Key,
                World = World,
                Local = local
            };
            from.KnownSystems.Add(c);
            to.KnowingSystems.Add(c);
            Database.Add(c);
            return c;
        }

        /// <summary>
        /// Creates a new vulnerability.
        /// </summary>
        /// <param name="system">System for vulnerability.</param>
        /// <param name="protocol">Vulnerability protocol (e.g. "ssh").</param>
        /// <param name="entryPoint">Vulnerability entrypoint/port (e.g. "22").</param>
        /// <param name="exploits">Exploit count.</param>
        /// <param name="cve">Optional CVE number (for fun).</param>
        /// <returns>Generated model.</returns>
        public VulnerabilityModel Vulnerability(SystemModel system, string protocol, string entryPoint,
            int exploits, string? cve = null)
        {
            var vuln = new VulnerabilityModel
            {
                Key = Guid.NewGuid(),
                World = World,
                System = system,
                Protocol = protocol,
                EntryPoint = entryPoint,
                Exploits = exploits,
                Cve = cve
            };
            system.Vulnerabilities.Add(vuln);
            Database.Add(vuln);
            return vuln;
        }

        /// <summary>
        /// Creates a new login.
        /// </summary>
        /// <param name="owner">System the login belongs to.</param>
        /// <param name="user">Username to make login for.</param>
        /// <param name="hash">Password hash.</param>
        /// <param name="salt">Password salt.</param>
        /// <param name="admin">If true, admin on system.</param>
        /// <param name="person">Person to associate login with.</param>
        /// <returns>Generated model.</returns>
        public LoginModel Login(SystemModel owner, string user, byte[] hash, byte[] salt, bool admin,
            PersonModel? person = null)
        {
            var login = new LoginModel
            {
                Key = Guid.NewGuid(),
                World = World,
                System = owner,
                User = user,
                Hash = hash,
                Salt = salt,
                Person = person?.Key ?? Guid.Empty,
                Admin = admin
            };
            owner.Logins.Add(login);
            Database.Add(login);
            return login;
        }

        /// <summary>
        /// Creates a folder.
        /// </summary>
        /// <param name="system">System file resides on.</param>
        /// <param name="login">File owner.</param>
        /// <param name="path">Target path.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">File already exists.</exception>
        public FileModel Folder(SystemModel system, LoginModel login, string path, bool hidden = false)
        {
            var (dir, name) = Executable.GetDirectoryAndName(path);
            return Folder(system, login, name, dir, hidden);
        }

        /// <summary>
        /// Creates a folder.
        /// </summary>
        /// <param name="system">System file resides on.</param>
        /// <param name="login">File owner.</param>
        /// <param name="name">Filename.</param>
        /// <param name="dir">Directory.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">File already exists.</exception>
        public FileModel Folder(SystemModel system, LoginModel login, string name, string dir, bool hidden = false)
        {
            if (system.Files.Any(f => f.Hidden == hidden && f.Path == dir && f.Name == name))
                throw new IOException($"The specified path already exists: {Executable.Combine(dir, name)}");
            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = FileModel.FileKind.Folder,
                Name = name,
                Path = dir,
                System = system,
                Owner = login,
                World = World,
                Hidden = hidden
            };

            // Generate dependent folders
            if (dir != "/")
            {
                (string? nPath, var nName) = (Executable.GetDirectoryName(dir)!, Executable.GetFileName(dir));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, login, nName, nPath, hidden);
            }

            system.Files.Add(model);
            Database.Add(model);
            return model;
        }

        /// <summary>
        /// Creates a file-backed file.
        /// </summary>
        /// <param name="system">System file resides on.</param>
        /// <param name="login">File owner.</param>
        /// <param name="path">Target path.</param>
        /// <param name="file">Source file.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">File already exists.</exception>
        public FileModel FileFile(SystemModel system, LoginModel login, string path, string file, bool hidden = false)
        {
            var (dir, name) = Executable.GetDirectoryAndName(path);
            return FileFile(system, login, name, dir, file, hidden);
        }

        /// <summary>
        /// Creates a file-backed file.
        /// </summary>
        /// <param name="system">System file resides on.</param>
        /// <param name="login">File owner.</param>
        /// <param name="name">Filename.</param>
        /// <param name="dir">Directory.</param>
        /// <param name="file">Source file.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">File already exists.</exception>
        public FileModel FileFile(SystemModel system, LoginModel login, string name, string dir, string file,
            bool hidden = false)
        {
            string target = Executable.Combine(dir, name);
            if (system.TryGetFile(target, login, out var result, out _, out _, hidden: hidden))
                throw new IOException(
                    $"The specified path already exists: {target}");
            if (result == ReadAccessResult.NotReadable)
                throw new IOException("Permission denied");
            if (system.GetUsedDiskSpace() + 1 > system.DiskCapacity)
                throw new IOException("Disk full.");

            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = FileModel.FileKind.FileFile,
                Name = name,
                Path = dir,
                System = system,
                Owner = login,
                World = World,
                Content = file,
                Hidden = hidden
            };

            // Generate dependent folders
            if (dir != "/")
            {
                (string? nPath, var nName) = (Executable.GetDirectoryName(dir)!, Executable.GetFileName(dir));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, login, nName, nPath, hidden);
            }

            system.Files.Add(model);
            Database.Add(model);
            return model;
        }

        /// <summary>
        /// Creates a text file.
        /// </summary>
        /// <param name="system">System file resides on.</param>
        /// <param name="login">File owner.</param>
        /// <param name="path">Target path.</param>
        /// <param name="content">Text content.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">File already exists.</exception>
        public FileModel TextFile(SystemModel system, LoginModel login, string path, string content,
            bool hidden = false)
        {
            var (dir, name) = Executable.GetDirectoryAndName(path);
            return TextFile(system, login, name, dir, content, hidden);
        }

        /// <summary>
        /// Creates a text file.
        /// </summary>
        /// <param name="system">System file resides on.</param>
        /// <param name="login">File owner.</param>
        /// <param name="name">Filename.</param>
        /// <param name="dir">Directory.</param>
        /// <param name="content">Text content.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">File already exists.</exception>
        public FileModel TextFile(SystemModel system, LoginModel login, string name, string dir, string content,
            bool hidden = false)
        {
            string target = Executable.Combine(dir, name);
            if (system.TryGetFile(target, login, out var result, out _, out _, hidden: hidden))
                throw new IOException(
                    $"The specified path already exists: {target}");
            if (result == ReadAccessResult.NotReadable)
                throw new IOException("Permission denied");
            if (system.GetUsedDiskSpace() + 1 > system.DiskCapacity)
                throw new IOException("Disk full.");

            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = FileModel.FileKind.TextFile,
                Name = name,
                Path = dir,
                System = system,
                Owner = login,
                World = World,
                Content = content,
                Hidden = hidden
            };

            // Generate dependent folders
            if (dir != "/")
            {
                (string? nPath, var nName) = (Executable.GetDirectoryName(dir)!, Executable.GetFileName(dir));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, login, nName, nPath, hidden);
            }

            system.Files.Add(model);
            Database.Add(model);
            return model;
        }

        /// <summary>
        /// Creates a program file.
        /// </summary>
        /// <param name="system">System file resides on.</param>
        /// <param name="login">File owner.</param>
        /// <param name="path">Target path.</param>
        /// <param name="progCode">ProgCode or hargv (hidden argv).</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">File already exists.</exception>
        public FileModel ProgFile(SystemModel system, LoginModel login, string path, string progCode,
            bool hidden = false)
        {
            var (dir, name) = Executable.GetDirectoryAndName(path);
            return ProgFile(system, login, name, dir, progCode, hidden);
        }

        /// <summary>
        /// Creates a program file.
        /// </summary>
        /// <param name="system">System file resides on.</param>
        /// <param name="login">File owner.</param>
        /// <param name="name">Filename.</param>
        /// <param name="dir">Directory.</param>
        /// <param name="progCode">ProgCode or hargv (hidden argv).</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">File already exists.</exception>
        public FileModel ProgFile(SystemModel system, LoginModel login, string name, string dir, string progCode,
            bool hidden = false)
        {
            string target = Executable.Combine(dir, name);
            if (system.TryGetFile(target, login, out var result, out _, out _, hidden: hidden))
                throw new IOException(
                    $"The specified path already exists: {target}");
            if (result == ReadAccessResult.NotReadable)
                throw new IOException("Permission denied");
            if (system.GetUsedDiskSpace() + 1 > system.DiskCapacity)
                throw new IOException("Disk full.");

            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = FileModel.FileKind.ProgFile,
                Name = name,
                Path = dir,
                System = system,
                Owner = login,
                World = World,
                Content = progCode,
                Hidden = hidden
            };

            // Generate dependent folders
            if (dir != "/")
            {
                (string? nPath, var nName) = (Executable.GetDirectoryName(dir)!, Executable.GetFileName(dir));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, login, nName, nPath, hidden);
            }

            system.Files.Add(model);
            Database.Add(model);
            return model;
        }

        /// <summary>
        /// Copies a file using file access.
        /// </summary>
        /// <param name="file">File to copy.</param>
        /// <param name="system">Target system.</param>
        /// <param name="login">Login to attempt operation with.</param>
        /// <param name="path">Target path.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">Thrown when there is an issue preventing the operation (e.g. access or already existing).</exception>
        public FileModel CopyFile(FileModel file, SystemModel system, LoginModel login, string path,
            bool hidden = false)
        {
            var (dir, name) = Executable.GetDirectoryAndName(path);
            return CopyFile(file, system, login, name, dir, hidden);
        }

        /// <summary>
        /// Copies a file using file access.
        /// </summary>
        /// <param name="file">File to copy.</param>
        /// <param name="system">Target system.</param>
        /// <param name="login">Login to attempt operation with.</param>
        /// <param name="name">Target filename.</param>
        /// <param name="dir">Target directory.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">Thrown when there is an issue preventing the operation (e.g. access or already existing).</exception>
        public FileModel CopyFile(FileModel file, SystemModel system, LoginModel login, string name, string dir,
            bool hidden = false)
        {
            string source = file.FullPath;
            string target = Executable.Combine(dir, name);
            // Prevent moving common root to subdirectory
            if (system == file.System && Executable.GetPathInCommon(source, target) == source)
                throw new IOException($"{source}: Cannot copy to {target}\n");
            if (!system.TryGetFile(source, login, out var result, out _, out _, hidden: hidden) ||
                !file.CanWrite(login))
                throw new IOException("Permission denied");
            if (system.TryGetFile(target, login, out result, out _, out _, hidden: hidden))
                throw new IOException(
                    $"The specified path already exists: {target}");
            if (result == ReadAccessResult.NotReadable)
                throw new IOException("Permission denied");

            var toCopy = new List<FileModel>();
            Queue<FileModel> queue = new();
            queue.Enqueue(file);
            while (queue.TryDequeue(out var f))
            {
                string fp = f.FullPath;
                if (!f.CanWrite(login)) throw new IOException("Permission denied");
                if (f.Kind == FileModel.FileKind.Folder)
                    foreach (var fx in system.Files.Where(ft => ft.Path == fp))
                        queue.Enqueue(fx);
                toCopy.Add(f);
            }

            if (system.GetUsedDiskSpace() +
                toCopy.Count > system.DiskCapacity)
                throw new IOException("Disk full.");

            // Generate dependent folders
            if (dir != "/")
            {
                (string? nPath, var nName) = (Executable.GetDirectoryName(dir)!, Executable.GetFileName(dir));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, login, nName, nPath, hidden);
            }

            string rootPath = file.FullPath;
            var copied = new List<FileModel>(toCopy.Count);
            foreach (var f in toCopy)
                copied.Add(new FileModel
                {
                    Key = Guid.NewGuid(),
                    Kind = f.Kind,
                    Name = f.Name,
                    Path = Executable.GetNormalized(
                        Executable.Combine(target, Path.GetRelativePath(rootPath, f.Path))),
                    System = system,
                    Owner = login,
                    World = World,
                    Content = f.Content,
                    Hidden = hidden
                });

            copied[0].Name = name;
            system.Files.UnionWith(copied);
            Database.AddBulk(copied);
            return copied[0];
        }

        /// <summary>
        /// Moves a file using file access.
        /// </summary>
        /// <param name="file">File to move.</param>
        /// <param name="system">Target system.</param>
        /// <param name="login">Login to attempt operation with.</param>
        /// <param name="path">Target path.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <exception cref="IOException">Thrown when there is an issue preventing the operation (e.g. access or already existing).</exception>
        public void MoveFile(FileModel file, SystemModel system, LoginModel login, string path,
            bool hidden = false)
        {
            var (dir, name) = Executable.GetDirectoryAndName(path);
            MoveFile(file, system, login, name, dir, hidden);
        }

        /// <summary>
        /// Moves a file using file access.
        /// </summary>
        /// <param name="file">File to move.</param>
        /// <param name="system">Target system.</param>
        /// <param name="login">Login to attempt operation with.</param>
        /// <param name="name">Target filename.</param>
        /// <param name="dir">Target directory.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <exception cref="IOException">Thrown when there is an issue preventing the operation (e.g. access or already existing).</exception>
        public void MoveFile(FileModel file, SystemModel system, LoginModel login, string name, string dir,
            bool hidden = false)
        {
            string source = file.FullPath;
            string target = Executable.Combine(dir, name);
            // Prevent moving common root to subdirectory
            if (system == file.System && Executable.GetPathInCommon(source, target) == source)
                throw new IOException($"{source}: Cannot move to {target}\n");
            if (!system.TryGetFile(source, login, out var result, out _, out _, hidden: hidden) ||
                !file.CanWrite(login))
                throw new IOException("Permission denied");
            if (system.TryGetFile(target, login, out result, out _, out _, hidden: hidden))
                throw new IOException(
                    $"The specified path already exists: {target}");
            if (result == ReadAccessResult.NotReadable)
                throw new IOException("Permission denied");

            var toModify = new List<FileModel>();
            Queue<FileModel> queue = new();
            queue.Enqueue(file);
            while (queue.TryDequeue(out var f))
            {
                string fp = f.FullPath;
                if (!f.CanWrite(login)) throw new IOException("Permission denied");
                if (f.Kind == FileModel.FileKind.Folder)
                    foreach (var fx in system.Files.Where(ft => ft.Path == fp))
                        queue.Enqueue(fx);
                toModify.Add(f);
            }

            // Generate dependent folders
            if (dir != "/")
            {
                var (parentPath, parentName) = Executable.GetDirectoryAndName(dir);
                if (system.Files.All(f => f.Hidden != hidden || f.Path != parentPath || f.Name != parentName))
                    Folder(system, login, parentName, parentPath, hidden);
            }

            string rootPath = file.FullPath;
            foreach (var f in toModify)
                f.Path = Executable.GetNormalized(
                    Executable.Combine(target, Path.GetRelativePath(rootPath, f.Path)));

            file.Name = name;
            Database.UpdateBulk(toModify);
        }

        /// <summary>
        /// Removes a system from the world.
        /// </summary>
        /// <param name="system">Model to remove.</param>
        /// <param name="isCascade">If true, does not directly delete from database.</param>
        public void RemoveSystem(SystemModel system, bool isCascade = false)
        {
            Database.DeleteBulk(system.KnownSystems);
            Database.DeleteBulk(system.KnowingSystems);
            system.KnownSystems.Clear();
            system.KnowingSystems.Clear();
            if (isCascade) return;
            Database.Delete(system);
        }

        /// <summary>
        /// Removes a person from the world.
        /// </summary>
        /// <param name="person">Model to remove.</param>
        /// <param name="isCascade">If true, does not directly delete from database.</param>
        public void RemovePerson(PersonModel person, bool isCascade = false)
        {
            foreach (var system in person.Systems)
                RemoveSystem(system, true);
            if (isCascade) return;
            Database.Delete(person);
        }

        /// <summary>
        /// Removes a file from the world, recursively.
        /// </summary>
        /// <param name="file">Model to remove.</param>
        /// <param name="login">Login to attempt operation with.</param>
        public void RemoveFile(FileModel file, LoginModel login)
        {
            var system = file.System;
            var toRemove = new List<FileModel>();
            Queue<FileModel> queue = new();
            queue.Enqueue(file);
            while (queue.TryDequeue(out var f))
            {
                string fp = f.FullPath;
                if (!f.CanWrite(login)) throw new IOException("Permission denied");
                if (f.Kind == FileModel.FileKind.Folder)
                    foreach (var fx in system.Files.Where(ft => ft.Path == fp))
                        queue.Enqueue(fx);
                toRemove.Add(f);
            }

            system.Files.ExceptWith(toRemove);
            Database.DeleteBulk(toRemove);
        }
    }
}

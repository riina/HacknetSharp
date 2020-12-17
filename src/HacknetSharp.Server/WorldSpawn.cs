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
        /// <param name="owner">File owner.</param>
        /// <param name="name">Filename.</param>
        /// <param name="path">Directory.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">File already exists.</exception>
        public FileModel Folder(SystemModel system, LoginModel owner, string name, string path, bool hidden = false)
        {
            if (system.Files.Any(f => f.Hidden == hidden && f.Path == path && f.Name == name))
                throw new IOException($"The specified path already exists: {Program.Combine(path, name)}");
            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = FileModel.FileKind.Folder,
                Name = name,
                Path = path,
                System = system,
                Owner = owner,
                World = World,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                (string? nPath, var nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            Database.Add(model);
            return model;
        }

        /// <summary>
        /// Creates a file-backed file.
        /// </summary>
        /// <param name="system">System file resides on.</param>
        /// <param name="owner">File owner.</param>
        /// <param name="name">Filename.</param>
        /// <param name="path">Directory.</param>
        /// <param name="file">Source file.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">File already exists.</exception>
        public FileModel FileFile(SystemModel system, LoginModel owner, string name, string path, string file,
            bool hidden = false)
        {
            if (system.Files.Any(f => f.Hidden == hidden && f.Path == path && f.Name == name))
                throw new IOException($"The specified path already exists: {Program.Combine(path, name)}");
            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = FileModel.FileKind.FileFile,
                Name = name,
                Path = path,
                System = system,
                Owner = owner,
                World = World,
                Content = file,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                (string? nPath, var nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            Database.Add(model);
            return model;
        }


        /// <summary>
        /// Creates a text file.
        /// </summary>
        /// <param name="system">System file resides on.</param>
        /// <param name="owner">File owner.</param>
        /// <param name="name">Filename.</param>
        /// <param name="path">Directory.</param>
        /// <param name="content">Text content.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">File already exists.</exception>
        public FileModel TextFile(SystemModel system, LoginModel owner, string name, string path, string content,
            bool hidden = false)
        {
            if (system.Files.Any(f => f.Hidden == hidden && f.Path == path && f.Name == name))
                throw new IOException($"The specified path already exists: {Program.Combine(path, name)}");
            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = FileModel.FileKind.TextFile,
                Name = name,
                Path = path,
                System = system,
                Owner = owner,
                World = World,
                Content = content,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                (string? nPath, var nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            Database.Add(model);
            return model;
        }


        /// <summary>
        /// Creates a program file.
        /// </summary>
        /// <param name="system">System file resides on.</param>
        /// <param name="owner">File owner.</param>
        /// <param name="name">Filename.</param>
        /// <param name="path">Directory.</param>
        /// <param name="progCode">ProgCode or hargv (hidden argv).</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">File already exists.</exception>
        public FileModel ProgFile(SystemModel system, LoginModel owner, string name, string path, string progCode,
            bool hidden = false)
        {
            if (system.Files.Any(f => f.Hidden == hidden && f.Path == path && f.Name == name))
                throw new IOException($"The specified path already exists: {Program.Combine(path, name)}");
            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = FileModel.FileKind.ProgFile,
                Name = name,
                Path = path,
                System = system,
                Owner = owner,
                World = World,
                Content = progCode,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                (string? nPath, var nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            Database.Add(model);
            return model;
        }


        /// <summary>
        /// Duplicates a file, non-recursive.
        /// </summary>
        /// <param name="system">System file resides on.</param>
        /// <param name="owner">File owner.</param>
        /// <param name="name">Filename.</param>
        /// <param name="path">Directory.</param>
        /// <param name="existing">Source file.</param>
        /// <param name="hidden">If true, hide from normal filesystem.</param>
        /// <returns>Generated model.</returns>
        /// <exception cref="IOException">File already exists.</exception>
        public FileModel Duplicate(SystemModel system, LoginModel owner, string name, string path, FileModel existing,
            bool hidden = false)
        {
            // does not support duplicating file tree. "yet" but also probably won't
            if (existing.Kind == FileModel.FileKind.Folder)
                throw new IOException($"Cannot copy folder {existing.FullPath}");
            if (system.Files.Any(f => f.Hidden == hidden && f.Path == path && f.Name == name))
                throw new IOException($"The specified path already exists: {Program.Combine(path, name)}");
            var model = new FileModel
            {
                Key = Guid.NewGuid(),
                Kind = existing.Kind,
                Name = name,
                Path = path,
                System = system,
                Owner = owner,
                World = World,
                Content = existing.Content,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                (string? nPath, var nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            Database.Add(model);
            return model;
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
        /// Moves a file using file access.
        /// </summary>
        /// <param name="file">File to move.</param>
        /// <param name="targetName">Target filename.</param>
        /// <param name="targetPath">Target directory.</param>
        /// <param name="login">Login to attempt operation with.</param>
        /// <param name="hidden">If true, hide from normal filesystem/</param>
        /// <exception cref="IOException">Thrown when there is an issue preventing the operation (e.g. access or already existing).</exception>
        public void MoveFile(FileModel file, string targetName, string targetPath, LoginModel login,
            bool hidden = false)
        {
            var system = file.System;
            if (!system.TryGetFile(file.FullPath, login, out var result, out _, out _, hidden: hidden) ||
                !file.CanWrite(login))
                throw new IOException("Permission denied");

            if (system.Files.Any(f => f.Hidden == hidden && f.Path == targetPath && f.Name == targetName))
                throw new IOException($"The specified path already exists: {Program.Combine(targetPath, targetName)}");
            system.TryGetFile(targetPath, login, out result, out _, out _, hidden: hidden);
            switch (result)
            {
                case ReadAccessResult.Readable:
                    // Not possible if fs search didn't find one
                    break;
                case ReadAccessResult.NotReadable:
                    throw new IOException("Permission denied");
                case ReadAccessResult.NoExist:
                    break;
            }

            // Generate dependent folders
            if (targetPath != "/")
            {
                var (parentPath, parentName) = SystemModel.GetDirectoryAndName(targetPath);
                if (system.Files.All(f => f.Hidden != hidden || f.Path != parentPath || f.Name != parentName))
                    Folder(system, login, parentName, parentPath, hidden);
            }

            if (file.Kind == FileModel.FileKind.Folder)
            {
                var toModify = new List<FileModel>();
                string rootPath = file.FullPath;
                Queue<FileModel> queue = new();
                queue.Enqueue(file);
                while (queue.TryDequeue(out var f))
                {
                    string fp = f.FullPath;
                    foreach (var fx in system.Files.Where(ft => ft.Path == fp))
                    {
                        if (!fx.CanWrite(login)) throw new IOException("Permission denied");
                        if (fx.Kind == FileModel.FileKind.Folder)
                            queue.Enqueue(fx);
                        toModify.Add(fx);
                    }
                }

                string targetFp = Program.GetNormalized(Program.Combine(targetPath, targetName));
                foreach (var f in toModify)
                    f.Path = Program.Combine(targetFp, Path.GetRelativePath(rootPath, f.Path));

                Database.UpdateBulk(toModify);
            }

            file.Path = targetPath;
            file.Name = targetName;
            Database.Update(file);
        }


        /// <summary>
        /// Removes a file from the world, recursively.
        /// </summary>
        /// <param name="file">Model to remove.</param>
        /// <param name="login">Login to attempt operation with.</param>
        public void RemoveFile(FileModel file, LoginModel login)
        {
            if (!file.CanWrite(login)) throw new IOException("Permission denied");
            string self = file.FullPath;
            var system = file.System;
            var tmp = new List<FileModel>(system.Files.Where(f => f.Path == self));
            foreach (var f in tmp)
                RemoveFile(f, login);
            system.Files.Remove(file);
            Database.Delete(file);
        }
    }
}

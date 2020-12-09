using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;

namespace HacknetSharp.Server
{
    public class WorldSpawn : Spawn
    {
        protected readonly WorldModel _world;

        public WorldSpawn(IServerDatabase database, WorldModel world) : base(database)
        {
            _world = world;
        }

        public PersonModel Person(string name, string userName, UserModel? user = null)
        {
            var person = new PersonModel
            {
                Key = Guid.NewGuid(),
                World = _world,
                Name = name,
                UserName = userName,
                Systems = new HashSet<SystemModel>(),
                User = user
            };
            user?.Identities.Add(person);
            _world.Persons.Add(person);
            _database.Add(person);
            return person;
        }

        private readonly Random _random = new Random();

        public unsafe SystemModel System(SystemTemplate template, PersonModel owner, byte[] hash, byte[] salt,
            IPAddressRange range)
        {
            if (!range.TryGetIPv4HostAndSubnetMask(out uint host, out uint subnetMask))
                throw new ApplicationException("Address range is not IPv4");
            uint resAddr;
            var systems = _world.Systems.Where(s => ((s.Address & subnetMask) ^ host) == 0).Select(s => s.Address)
                .ToHashSet();
            if (range.PrefixBits < 32)
            {
                if (systems.Count >= 1 << (32 - range.PrefixBits))
                    throw new ApplicationException("Specified range has been saturated");

                uint invSubnetMask = ~subnetMask;
                uint gen;
                Span<byte> span = new Span<byte>((byte*)&gen, 4);
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
                    throw new ApplicationException("System with target address already exists");
                resAddr = host;
            }

            return System(template, owner, hash, salt, resAddr);
        }

        public SystemModel System(SystemTemplate template, PersonModel owner, byte[] hash, byte[] salt, uint address)
        {
            var system = new SystemModel
            {
                Address = address,
                Key = Guid.NewGuid(),
                World = _world,
                Owner = owner,
                Files = new HashSet<FileModel>(),
                Logins = new HashSet<LoginModel>(),
                KnownSystems = new HashSet<KnownSystemModel>(),
                KnowingSystems = new HashSet<KnownSystemModel>(),
                Vulnerabilities = new HashSet<VulnerabilityModel>(),
                BootTime = _world.Now
            };
            template.ApplyTemplate(this, system, owner, hash, salt);
            owner.Systems.Add(system);
            _world.Systems.Add(system);
            _database.Add(system);
            return system;
        }

        public KnownSystemModel Connection(SystemModel from, SystemModel to, bool local)
        {
            var c = new KnownSystemModel
            {
                From = from,
                FromKey = from.Key,
                To = to,
                ToKey = to.Key,
                World = _world,
                Local = local
            };
            from.KnownSystems.Add(c);
            to.KnowingSystems.Add(c);
            _database.Add(c);
            return c;
        }

        public VulnerabilityModel Vulnerability(SystemModel system)
        {
            var vuln = new VulnerabilityModel {Key = Guid.NewGuid(), World = _world, System = system};
            system.Vulnerabilities.Add(vuln);
            _database.Add(vuln);
            return vuln;
        }

        public LoginModel Login(SystemModel owner, string user, byte[] hash, byte[] salt, bool admin,
            PersonModel? person = null)
        {
            var login = new LoginModel
            {
                Key = Guid.NewGuid(),
                World = _world,
                System = owner,
                User = user,
                Hash = hash,
                Salt = salt,
                Person = person?.Key ?? Guid.Empty,
                Admin = admin
            };
            owner.Logins.Add(login);
            _database.Add(login);
            return login;
        }

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
                World = _world,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            _database.Add(model);
            return model;
        }

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
                World = _world,
                Content = file,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            _database.Add(model);
            return model;
        }

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
                World = _world,
                Content = content,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            _database.Add(model);
            return model;
        }

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
                World = _world,
                Content = progCode,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            _database.Add(model);
            return model;
        }

        public FileModel Duplicate(SystemModel system, LoginModel owner, string name, string path, FileModel existing,
            bool hidden = false)
        {
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
                World = _world,
                Content = existing.Content,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            _database.Add(model);
            return model;
        }

        public void RemoveSystem(SystemModel system, bool isCascade = false)
        {
            _database.DeleteBulk(system.KnownSystems);
            _database.DeleteBulk(system.KnowingSystems);
            system.KnownSystems.Clear();
            system.KnowingSystems.Clear();
            if (isCascade) return;
            _database.Delete(system);
        }

        public void RemovePerson(PersonModel person, bool isCascade = false)
        {
            foreach (var system in person.Systems)
                RemoveSystem(system, true);
            if (isCascade) return;
            _database.Delete(person);
        }

        public void MoveFile(FileModel file, string targetPath, string targetName, LoginModel login,
            bool hidden = false)
        {
            if (!file.CanWrite(login)) throw new IOException("Permission denied");
            var system = file.System;
            if (system.Files.Any(f => f.Hidden == hidden && f.Path == targetPath && f.Name == targetName))
                throw new IOException($"The specified path already exists: {Program.Combine(targetPath, targetName)}");
            system.TryGetWithAccess(targetPath, login, out var result, out var closest, hidden);
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
                Queue<FileModel> queue = new Queue<FileModel>();
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

                _database.EditBulk(toModify);
            }

            file.Path = targetPath;
            file.Name = targetName;
            _database.Edit(file);
        }

        public void RemoveFile(FileModel file, LoginModel login)
        {
            if (!file.CanWrite(login)) throw new IOException("Permission denied");
            string self = file.FullPath;
            var system = file.System;
            var tmp = new List<FileModel>(system.Files.Where(f => f.Path == self));
            foreach (var f in tmp)
                RemoveFile(f, login);
            system.Files.Remove(file);
            _database.Delete(file);
        }
    }
}

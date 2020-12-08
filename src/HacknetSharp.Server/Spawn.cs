using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;

namespace HacknetSharp.Server
{
    public class Spawn
    {
        public PlayerModel Player(IServerDatabase database, UserModel context)
        {
            var player = new PlayerModel {User = context, Key = context.Key, Identities = new HashSet<PersonModel>()};
            context.Player = player;
            database.Add(player);
            return player;
        }

        public PersonModel Person(IServerDatabase database, WorldModel context, string name, string userName,
            PlayerModel? player = null)
        {
            var person = new PersonModel
            {
                Key = Guid.NewGuid(),
                World = context,
                Name = name,
                UserName = userName,
                Systems = new HashSet<SystemModel>(),
                Player = player
            };
            player?.Identities.Add(person);
            context.Persons.Add(person);
            database.Add(person);
            return person;
        }

        private readonly Random _random = new Random();

        public unsafe SystemModel System(IServerDatabase database, WorldModel context, SystemTemplate template,
            PersonModel owner, byte[] hash, byte[] salt, IPAddressRange range)
        {
            if (!range.TryGetIPv4HostAndSubnetMask(out uint host, out uint subnetMask))
                throw new ApplicationException("Address range is not IPv4");
            uint resAddr;
            var systems = context.Systems.Where(s => ((s.Address & subnetMask) ^ host) == 0).Select(s => s.Address)
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

            return System(database, context, template, owner, hash, salt, resAddr);
        }

        public SystemModel System(IServerDatabase database, WorldModel context, SystemTemplate template,
            PersonModel owner, byte[] hash, byte[] salt, uint address)
        {
            var system = new SystemModel
            {
                Address = address,
                Key = Guid.NewGuid(),
                World = context,
                Owner = owner,
                Files = new HashSet<FileModel>(),
                Logins = new HashSet<LoginModel>(),
                KnownSystems = new HashSet<KnownSystemModel>(),
                KnowingSystems = new HashSet<KnownSystemModel>(),
                Vulnerabilities = new HashSet<VulnerabilityModel>(),
                BootTime = context.Now
            };
            template.ApplyTemplate(database, this, context, system, owner, hash, salt);
            owner.Systems.Add(system);
            context.Systems.Add(system);
            database.Add(system);
            return system;
        }

        public KnownSystemModel Connection(IServerDatabase database, SystemModel from, SystemModel to)
        {
            var c = new KnownSystemModel {From = from, FromKey = from.Key, To = to, ToKey = to.Key};
            from.KnownSystems.Add(c);
            to.KnowingSystems.Add(c);
            database.Add(c);
            return c;
        }

        public VulnerabilityModel Vulnerability(IServerDatabase database, WorldModel context, SystemModel system)
        {
            var vuln = new VulnerabilityModel {Key = Guid.NewGuid(), World = context, System = system};
            system.Vulnerabilities.Add(vuln);
            database.Add(vuln);
            return vuln;
        }

        public LoginModel Login(IServerDatabase database, WorldModel context, SystemModel owner, string user,
            byte[] hash, byte[] salt, bool admin,
            PersonModel? person = null)
        {
            var login = new LoginModel
            {
                Key = Guid.NewGuid(),
                World = context,
                System = owner,
                User = user,
                Hash = hash,
                Salt = salt,
                Person = person?.Key ?? Guid.Empty,
                Admin = admin
            };
            owner.Logins.Add(login);
            database.Add(login);
            return login;
        }

        public FileModel Folder(IServerDatabase database, WorldModel context, SystemModel system, LoginModel owner,
            string name, string path, bool hidden = false)
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
                World = context,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(database, context, system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            database.Add(model);
            return model;
        }

        public FileModel FileFile(IServerDatabase database, WorldModel context, SystemModel system, LoginModel owner,
            string name, string path, string file, bool hidden = false)
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
                World = context,
                Content = file,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(database, context, system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            database.Add(model);
            return model;
        }

        public FileModel TextFile(IServerDatabase database, WorldModel context, SystemModel system, LoginModel owner,
            string name, string path, string content, bool hidden = false)
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
                World = context,
                Content = content,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(database, context, system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            database.Add(model);
            return model;
        }

        public FileModel ProgFile(IServerDatabase database, WorldModel context, SystemModel system, LoginModel owner,
            string name, string path, string progCode, bool hidden = false)
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
                World = context,
                Content = progCode,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(database, context, system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            database.Add(model);
            return model;
        }

        public FileModel Duplicate(IServerDatabase database, WorldModel context, SystemModel system, LoginModel owner,
            string name, string path, FileModel existing, bool hidden = false)
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
                World = context,
                Content = existing.Content,
                Hidden = hidden
            };

            // Generate dependent folders
            if (path != "/")
            {
                var (nPath, nName) = (Program.GetDirectoryName(path)!, Program.GetFileName(path));
                if (!system.Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName))
                    Folder(database, context, system, owner, nName, nPath, hidden);
            }

            system.Files.Add(model);
            database.Add(model);
            return model;
        }

        public WorldModel World(IServerDatabase database, string name, TemplateGroup templates, WorldTemplate template)
        {
            var world = new WorldModel
            {
                Key = Guid.NewGuid(),
                Name = name,
                Persons = new HashSet<PersonModel>(),
                Systems = new HashSet<SystemModel>()
            };
            template.ApplyTemplate(database, this, templates, world);
            database.Add(world);
            return world;
        }
    }
}

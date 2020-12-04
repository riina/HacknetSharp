using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using HacknetSharp.Server;
using HacknetSharp.Server.Templates;
using hss.Core;
using NUnit.Framework;

namespace HacknetSharp.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test_UtilSubclass()
        {
            Assert.IsTrue(ServerUtil.IsSubclass(typeof(Model<>), typeof(SubModel)));
            Assert.IsFalse(ServerUtil.IsSubclass(typeof(Model<>), typeof(int)));
            Assert.IsFalse(ServerUtil.IsSubclass(typeof(int), typeof(int)));
            Assert.IsFalse(ServerUtil.IsSubclass(typeof(Model<>), typeof(Model<>)));
            Assert.IsTrue(ServerUtil.IsSubclass(typeof(object), typeof(string)));
            Assert.IsTrue(ServerUtil.IsSubclass(typeof(object), typeof(List<int>)));
        }

        private class SubModel : Model<int>
        {
        }

        [Test]
        public void Test_Paths()
        {
            Assert.AreEqual("/", Program.GetNormalized("/test/.."));
            Assert.AreEqual("/test/path", Program.GetNormalized("/test/../test/path/"));
            Assert.AreEqual("/test", Program.GetDirectoryName("/test/me"));
            Assert.AreEqual("/test", Program.GetNormalized(Program.GetDirectoryName("/test/me/../harder")!));
            Assert.AreEqual(null, Program.GetDirectoryName("/"));
            Assert.AreEqual("", Program.GetFileName("/"));
            Assert.AreEqual("/", Program.Combine("/", ""));
            Assert.AreEqual("/", Program.GetNormalized(Program.Combine("/", "")));
            Assert.AreEqual(("/", ""), HacknetSharp.Server.System.GetDirectoryAndName("/"));
            Assert.AreEqual(("/", "shadow"), HacknetSharp.Server.System.GetDirectoryAndName("/shadow"));
            Assert.AreEqual(("/shadow", "absorber"),
                HacknetSharp.Server.System.GetDirectoryAndName("/shadow/absorber"));
        }

        private readonly Dictionary<string, object>
            _templates =
                new Dictionary<string, object>
                {
                    {
                        "system",
                        new SystemTemplate
                        {
                            NameFormat = "{0}_HOMEBASE",
                            OsName = "EncomOS",
                            Users = new List<string>(new[] {"daphne:legacy", "samwise:genshin"}),
                            Filesystem = new List<string>(new[]
                            {
                                "fold*+*:/bin", "fold:/etc", "fold:/home", "fold*+*:/lib", "fold:/mnt",
                                "fold+++:/root", "fold:/usr", "fold:/usr/bin", "fold:/usr/lib", "fold:/usr/local",
                                "fold:/usr/share", "fold:/var", "fold:/var/spool",
                                "text:\"/home/samwise/read me.txt\" mr. frodo, sir!",
                                "file:/home/samwise/image.png misc/image.png", "prog:/bin/cat core:cat",
                                "prog:/bin/cd core:cd", "prog:/bin/ls core:ls"
                            })
                        }
                    },
                    {
                        "person", new PersonTemplate
                        {
                            Usernames = new List<string> {"locke", "bacon", "hayleyk653"},
                            Passwords = new List<string> {"misterchef", "baconbacon", "isucklol"},
                            EmailProviders =
                                new List<string> {"hentaimail.net", "thisisnotaproblem.org", "fbiopenup.gov"},
                            SystemTemplates = new List<string> {"systemTemplate2"}
                        }
                    },
                    {
                        "world",
                        new WorldTemplate
                        {
                            Label = "Liyue kinda sux",
                            PlayerSystemTemplate = "playerTemplate",
                            StartupCommandLine = "echo \"Starting shell...\"",
                            PlayerAddressRange = Constants.DefaultAddressRange,
                            Generators = new List<WorldTemplate.Generator>
                            {
                                new WorldTemplate.Generator {Count = 3, PersonTemplate = "personTemplate2"}
                            }
                        }
                    }
                };

        private class DummyServerDatabase : IServerDatabase
        {
            public Task<TResult> GetAsync<TKey, TResult>(TKey key)
                where TKey : IEquatable<TKey> where TResult : Model<TKey> => throw new NotSupportedException();

            public Task<List<TResult>> GetBulkAsync<TKey, TResult>(ICollection<TKey> keys)
                where TKey : IEquatable<TKey> where TResult : Model<TKey> => throw new NotSupportedException();

            public void Add<TEntry>(TEntry entity) where TEntry : notnull
            {
            }

            public void AddBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull
            {
            }

            public void Edit<TEntry>(TEntry entity) where TEntry : notnull
            {
            }

            public void EditBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull
            {
            }

            public void Delete<TEntry>(TEntry entity) where TEntry : notnull
            {
            }

            public void DeleteBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull
            {
            }

            public Task SyncAsync() => Task.CompletedTask;
        }

        [Test]
        public void Test_Spawning()
        {
            var templates = new TemplateGroup();
            // Borrow sample template
            var system1Template = (SystemTemplate)_templates["system"];
            templates.SystemTemplates.Add("systemTemplate1", system1Template);
            templates.SystemTemplates.Add("systemTemplate2", system1Template);

            var personTemplate2 = (PersonTemplate)_templates["person"];
            templates.PersonTemplates.Add("personTemplate2", personTemplate2);

            var worldTemplate = (WorldTemplate)_templates["world"];
            //templates.WorldTemplates.Add("worldTemplate", worldTemplate);

            var spawn = new Spawn();

            IServerDatabase database = new DummyServerDatabase();

            var worldModel = spawn.World(database, "za warudo", templates, worldTemplate);

            var person1Model = spawn.Person(database, worldModel, "Jacob Keyes", "jacobkeyes");
            (byte[] person1Hash, byte[] person1Salt) = CommonUtil.HashPassword("miranda");

            var system1Model = spawn.System(database, worldModel, system1Template, person1Model, person1Hash,
                person1Salt,
                new IPAddressRange(Constants.DefaultAddressRange));

            // Initially have 3 systems from generator, add custom system

            Assert.AreEqual(1 + 3, worldModel.Systems.Count);

            Assert.AreEqual("jacobkeyes_HOMEBASE", system1Model.Name);
            Assert.AreEqual(1,
                system1Model.Logins.Count(l => l.User == "jacobkeyes" && l.System == system1Model &&
                                               l.World == worldModel &&
                                               l.Hash == person1Hash && l.Salt == person1Salt));
            Assert.AreEqual(1,
                system1Model.Logins.Count(l => l.User == "samwise" && l.System == system1Model &&
                                               l.World == worldModel));
        }


        [Test]
        public void TestCidr()
        {
            IPAddressRange range = new IPAddressRange("192.168.0.0/24");
            Assert.IsTrue(range.Contains(IPAddress.Parse("192.168.0.1")));
            Assert.IsFalse(range.Contains(IPAddress.Parse("192.168.1.0")));
            IPAddressRange range2 = new IPAddressRange("192.168.0.0/23");
            Assert.IsTrue(range2.Contains(IPAddress.Parse("192.168.0.1")));
            Assert.IsTrue(range2.Contains(IPAddress.Parse("192.168.1.1")));
            Assert.IsFalse(range2.Contains(IPAddress.Parse("192.168.2.1")));
            Assert.IsTrue(range.TryGetIPv4HostAndSubnetMask(out uint host, out uint subnetMask));
            Assert.AreEqual(0xc0_a8_00_00, host);
            Assert.AreEqual(0xff_ff_ff_00, subnetMask);
        }

        [Test]
        public void TestReplacements()
        {
            var dict = new Dictionary<string, string>();
            string str = "daisy johnson {skye}";
            Assert.AreEqual(str, str.ApplyReplacements(dict));
            dict["skye"] = "quake";
            Assert.AreEqual("daisy johnson quake", str.ApplyReplacements(dict));
            dict["blergh1"] = "shots";
            dict["blergh2"] = "fired";
            Assert.AreEqual("shots fired", "{blergh1} {blergh2}".ApplyReplacements(dict));
        }
    }
}

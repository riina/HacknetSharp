using System.Collections.Generic;
using System.Linq;
using HacknetSharp.Server;
using HacknetSharp.Server.Common;
using HacknetSharp.Server.Common.Templates;
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
            Assert.AreEqual(("/", ""), Server.Common.System.GetDirectoryAndName("/"));
            Assert.AreEqual(("/", "shadow"), Server.Common.System.GetDirectoryAndName("/shadow"));
            Assert.AreEqual(("/shadow", "absorber"), Server.Common.System.GetDirectoryAndName("/shadow/absorber"));
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
                            StartupProgram = "wish",
                            StartupCommandLine = "echo \"Configuring system...\"",
                            Generators = new List<WorldTemplate.Generator>
                            {
                                new WorldTemplate.Generator {Count = 3, PersonTemplate = "personTemplate2"}
                            }
                        }
                    }
                };

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

            var worldModel = spawn.World("za warudo", templates, worldTemplate);

            var person1Model = spawn.Person(worldModel, "Jacob Keyes", "jacobkeyes");
            (byte[] person1Hash, byte[] person1Salt) = CommonUtil.HashPassword("miranda");

            var system1Model = spawn.System(worldModel, system1Template, person1Model, person1Hash, person1Salt);

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
    }
}

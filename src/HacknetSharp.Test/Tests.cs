using System.Collections.Generic;
using HacknetSharp.Server;
using HacknetSharp.Server.Common;
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
    }
}

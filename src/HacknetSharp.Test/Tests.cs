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
    }
}

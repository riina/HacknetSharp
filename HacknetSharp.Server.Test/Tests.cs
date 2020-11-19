using System.Collections.Generic;
using HacknetSharp.Server.Common;
using NUnit.Framework;

namespace HacknetSharp.Server.Test
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
            Assert.IsTrue(Util.IsSubclass(typeof(Model<>), typeof(SubModel)));
            Assert.IsFalse(Util.IsSubclass(typeof(Model<>), typeof(int)));
            Assert.IsTrue(Util.IsSubclass(typeof(object), typeof(string)));
            Assert.IsTrue(Util.IsSubclass(typeof(object), typeof(List<int>)));
        }

        private class SubModel : Model<int>
        {
        }
    }
}

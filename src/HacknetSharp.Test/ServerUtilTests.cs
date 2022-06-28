using System.Collections.Generic;
using HacknetSharp.Server;
using NUnit.Framework;

namespace HacknetSharp.Test
{
    public class ServerUtilTests
    {
        [Test]
        public void SubclassCheck_Works()
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

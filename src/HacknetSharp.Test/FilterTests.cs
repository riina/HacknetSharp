using HacknetSharp.Server;
using NUnit.Framework;

namespace HacknetSharp.Test
{
    public class FilterTests
    {
        [Test]
        public void Filter_ForIP_Works()
        {
            var filter = PathFilter.GenerateFilter(new[] { "*.69", "*.78.*" }, true);
            Assert.IsTrue(filter.Test("69.69.69.69"));
            Assert.IsTrue(filter.Test("12.34.78.99"));
            Assert.IsFalse(filter.Test("12.34.77.99"));
        }
    }
}

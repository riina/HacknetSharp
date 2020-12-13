using System.Collections.Generic;
using System.Net;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;
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
            Assert.AreEqual(("/", ""), SystemModel.GetDirectoryAndName("/"));
            Assert.AreEqual(("/", "shadow"), SystemModel.GetDirectoryAndName("/shadow"));
            Assert.AreEqual(("/shadow", "absorber"),
                SystemModel.GetDirectoryAndName("/shadow/absorber"));
        }


        [Test]
        public void Test_Cidr()
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
            Assert.IsTrue(range2.TryGetIPv4HostAndSubnetMask(out uint host2, out uint subnetMask2));
            Assert.AreEqual(0xc0_a8_00_00, host2);
            Assert.AreEqual(0xff_ff_fe_00, subnetMask2);
            IPAddressRange selfish = new IPAddressRange("0.0.0.69");
            Assert.AreEqual("0.0.0.69/32", selfish.ToString());
            Assert.AreEqual(new IPAddressRange("192.168.0.69"), selfish.OnHost(range));
            Assert.AreEqual(new IPAddressRange("192.168.0.69"), selfish.OnHost(range2));
        }

        [Test]
        public void Test_Replacements()
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

        [Test]
        public void Test_Filter()
        {
            var filter = PathFilter.GenerateFilter(new[] {"*.69", "*.78.*"}, true);
            Assert.IsTrue(filter.Test("69.69.69.69"));
            Assert.IsTrue(filter.Test("12.34.78.99"));
            Assert.IsFalse(filter.Test("12.34.77.99"));
        }

        [Test]
        public void Test_Args()
        {
            Assert.AreEqual(new[] {"li\"yue", "\"ki \\nda\" ", "\"sucks", "bro\"", "\"lm\"ao\""},
                ServerUtil.SplitCommandLine("li\\\"yue \"\\\"ki \\nda\\\" \" \\\"sucks bro\\\" \\\"lm\\\"ao\\\""));
        }
    }
}

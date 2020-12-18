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
            Assert.AreEqual("/", Executable.GetNormalized("/test/.."));
            Assert.AreEqual("/test/path", Executable.GetNormalized("/test/../test/path/"));
            Assert.AreEqual("/test", Executable.GetDirectoryName("/test/me"));
            Assert.AreEqual("/test", Executable.GetNormalized(Executable.GetDirectoryName("/test/me/../harder")!));
            Assert.AreEqual(null, Executable.GetDirectoryName("/"));
            Assert.AreEqual("", Executable.GetFileName("/"));
            Assert.AreEqual("/", Executable.Combine("/", ""));
            Assert.AreEqual("/", Executable.GetNormalized(Executable.Combine("/", "")));
            Assert.AreEqual(("/", ""), Executable.GetDirectoryAndName("/"));
            Assert.AreEqual(("/", "shadow"), Executable.GetDirectoryAndName("/shadow"));
            Assert.AreEqual(("/shadow", "absorber"),
                Executable.GetDirectoryAndName("/shadow/absorber"));
            Assert.AreEqual("/", Executable.GetPathInCommon("/b", "/"));
            Assert.AreEqual("/", Executable.GetPathInCommon("/b", "/z"));
            Assert.AreEqual("/faber", Executable.GetPathInCommon("/faber/of/will", "/faber/and/might"));
            Assert.AreEqual("/faber", Executable.GetPathInCommon("/faber/and/might", "/faber/of/will"));
            Assert.AreEqual("/dark/ness", Executable.GetPathInCommon("/dark/ness/lalatina", "/dark/ness/overlord"));
            Assert.AreEqual("/well", Executable.GetPathInCommon("/well/waitforit", "/well/waitfor"));
        }


        [Test]
        public void Test_Cidr()
        {
            IPAddressRange range = new("192.168.0.0/24");
            Assert.IsTrue(range.Contains(IPAddress.Parse("192.168.0.1")));
            Assert.IsFalse(range.Contains(IPAddress.Parse("192.168.1.0")));
            IPAddressRange range2 = new("192.168.0.0/23");
            Assert.IsTrue(range2.Contains(IPAddress.Parse("192.168.0.1")));
            Assert.IsTrue(range2.Contains(IPAddress.Parse("192.168.1.1")));
            Assert.IsFalse(range2.Contains(IPAddress.Parse("192.168.2.1")));
            Assert.IsTrue(range.TryGetIPv4HostAndSubnetMask(out uint host, out uint subnetMask));
            Assert.AreEqual(0xc0_a8_00_00, host);
            Assert.AreEqual(0xff_ff_ff_00, subnetMask);
            Assert.IsTrue(range2.TryGetIPv4HostAndSubnetMask(out uint host2, out uint subnetMask2));
            Assert.AreEqual(0xc0_a8_00_00, host2);
            Assert.AreEqual(0xff_ff_fe_00, subnetMask2);
            IPAddressRange selfish = new("0.0.0.69");
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

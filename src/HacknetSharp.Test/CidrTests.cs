using System.Net;
using HacknetSharp.Server;
using NUnit.Framework;

namespace HacknetSharp.Test
{
    public class CidrTests
    {
        [Test]
        public void Cidr_Simple_Works()
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
    }
}

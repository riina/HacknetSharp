/*
MIT License
Copyright (c) 2020 riina
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace HacknetSharp.Server.Common
{
    /// <summary>
    /// Represents a CIDR IP range (or a single IP address)
    /// </summary>
    /// <remarks>
    /// Internally, the prefix is stored in a 16-byte buffer purely for having a single struct to use between IPv4 and IPv6
    /// </remarks>
    public unsafe struct IPAddressRange
    {
#pragma warning disable 649
        private fixed byte _prefix[16];
#pragma warning restore 649
        public readonly AddressFamily AddressFamily;
        public readonly int PrefixBits;
        public readonly byte PrefixBitmask;

        public IPAddressRange(string cidrString)
        {
            int location = cidrString.LastIndexOf('/');
            IPAddress? address;
            int prefix;
            Span<byte> addrBytes = stackalloc byte[16];
            if (location == -1)
            {
                // Single IP address

                // Parse address
                if (!IPAddress.TryParse(cidrString, out address))
                    throw new ArgumentException($"Failed to parse IPAddress from {cidrString}");

                // Get address bytes
                if (!address.TryWriteBytes(addrBytes, out int addrLen))
                    throw new ArgumentException("Failed to write address bytes");

                // Get prefix length
                prefix = addrLen;
            }
            else
            {
                // CIDR address range

                // Parse address
                if (!IPAddress.TryParse(cidrString.AsSpan(0, location), out address))
                    throw new ArgumentException($"Failed to parse IPAddress from {cidrString}");

                // Get address bytes
                if (!address.TryWriteBytes(addrBytes, out int addrLen))
                    throw new ArgumentException("Failed to write address bytes");

                // get prefix length
                if (!int.TryParse(cidrString.AsSpan(location + 1), out prefix))
                    throw new ArgumentException(
                        $"cidr string routing parse failed, input string: {cidrString.Substring(location + 1)}");
                if (addrLen * 8 < prefix) throw new ArgumentException("Mask length exceeded address bit length");
            }

            AddressFamily = address.AddressFamily;
            PrefixBits = prefix;
            int prefixBytes = prefix / 8;
            PrefixBitmask = (byte)(0xff << (8 - prefix % 8));
            fixed (byte* pp = _prefix)
                addrBytes.Slice(0, prefixBytes).CopyTo(new Span<byte>(pp, 16));
            if (PrefixBitmask != 0)
                _prefix[prefixBytes] = (byte)(PrefixBitmask & addrBytes[prefixBytes]);
            AddressFamily = address.AddressFamily;
        }

        public bool Contains(IPAddress address)
        {
            if (address.AddressFamily != AddressFamily) return false;
            if (PrefixBits == 0) return true;
            int prefixBytes = PrefixBits / 8;
            Span<byte> addrBytes = stackalloc byte[16];
            if (!address.TryWriteBytes(addrBytes, out int _))
                throw new ArgumentException("Failed to write address bytes");
            fixed (byte* pp = _prefix)
                if (!addrBytes.Slice(0, prefixBytes).SequenceEqual(new Span<byte>(pp, prefixBytes)))
                    return false;
            return PrefixBitmask == 0 || _prefix[prefixBytes] == (PrefixBitmask & addrBytes[prefixBytes]);
        }

        public (uint host, uint subnetMask) GetIPv4HostAndSubnetMask()
        {
            if (AddressFamily != AddressFamily.InterNetwork) throw new InvalidOperationException();
            uint netMask = ~(uint)0 << (32 - PrefixBits);
            if (PrefixBitmask != 0)
                netMask |= (uint)(PrefixBitmask << (24 - PrefixBits));
            uint host;
            fixed (byte* pp = _prefix)
                host = BinaryPrimitives.ReadUInt32BigEndian(new Span<byte>(pp, 4));
            return (host, netMask);
        }
    }
}

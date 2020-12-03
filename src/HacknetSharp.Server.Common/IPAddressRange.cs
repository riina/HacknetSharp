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
            int prefixBits;
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
                prefixBits = addrLen * 8;
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
                if (!int.TryParse(cidrString.AsSpan(location + 1), out prefixBits))
                    throw new ArgumentException(
                        $"cidr string routing parse failed, input string: {cidrString.Substring(location + 1)}");
                if (addrLen * 8 < prefixBits) throw new ArgumentException("Mask length exceeded address bit length");
            }

            PrefixBits = prefixBits;
            int prefixBytes = prefixBits / 8;
            PrefixBitmask = (byte)(0xff << (8 - prefixBits % 8));
            fixed (byte* pp = _prefix)
                addrBytes.Slice(0, prefixBytes).CopyTo(new Span<byte>(pp, 16));
            if (PrefixBitmask != 0)
                _prefix[prefixBytes] = (byte)(PrefixBitmask & addrBytes[prefixBytes]);
            AddressFamily = address.AddressFamily;
        }

        public IPAddressRange(IPAddress address)
        {
            int prefixBits;
            Span<byte> addrBytes = stackalloc byte[16];

            // Single IP address

            // Get address bytes
            if (!address.TryWriteBytes(addrBytes, out int addrLen))
                throw new ArgumentException("Failed to write address bytes");

            // Get prefix length
            prefixBits = addrLen * 8;

            PrefixBits = prefixBits;
            int prefixBytes = prefixBits / 8;
            PrefixBitmask = (byte)(0xff << (8 - prefixBits % 8));
            fixed (byte* pp = _prefix)
                addrBytes.Slice(0, prefixBytes).CopyTo(new Span<byte>(pp, 16));
            if (PrefixBitmask != 0)
                _prefix[prefixBytes] = (byte)(PrefixBitmask & addrBytes[prefixBytes]);
            AddressFamily = address.AddressFamily;
        }

        private IPAddressRange(Span<byte> prefix, AddressFamily addressFamily, int prefixBits, byte prefixBitmask)
        {
            fixed (byte* pp = _prefix) prefix.CopyTo(new Span<byte>(pp, 16));
            AddressFamily = addressFamily;
            PrefixBits = prefixBits;
            PrefixBitmask = prefixBitmask;
        }

        public static bool TryParse(string cidrString, bool allowRange, out IPAddressRange value)
        {
            int location = cidrString.LastIndexOf('/');
            IPAddress? address;
            int prefixBits;
            Span<byte> addrBytes = stackalloc byte[16];
            if (location == -1)
            {
                // Single IP address

                // Parse address
                if (!IPAddress.TryParse(cidrString, out address))
                {
                    value = default;
                    return false;
                }

                // Get address bytes
                if (!address.TryWriteBytes(addrBytes, out int addrLen))
                {
                    value = default;
                    return false;
                }

                // Get prefix length
                prefixBits = addrLen * 8;
            }
            else
            {
                // CIDR address range
                if (!allowRange)
                {
                    value = default;
                    return false;
                }

                // Parse address
                if (!IPAddress.TryParse(cidrString.AsSpan(0, location), out address))
                {
                    value = default;
                    return false;
                }

                // Get address bytes
                if (!address.TryWriteBytes(addrBytes, out int addrLen))
                {
                    value = default;
                    return false;
                }

                // get prefix length
                if (!int.TryParse(cidrString.AsSpan(location + 1), out prefixBits) || addrLen * 8 < prefixBits)
                {
                    value = default;
                    return false;
                }
            }

            int prefixBytes = prefixBits / 8;
            byte prefixBitmask = (byte)(0xff << (8 - prefixBits % 8));
            Span<byte> prefix = stackalloc byte[16];
            addrBytes.Slice(0, prefixBytes).CopyTo(prefix);
            if (prefixBitmask != 0)
                prefix[prefixBytes] = (byte)(prefixBitmask & addrBytes[prefixBytes]);
            var addressFamily = address.AddressFamily;

            value = new IPAddressRange(prefix, addressFamily, prefixBits, prefixBitmask);
            return true;
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

        public bool TryGetIPv4HostAndSubnetMask(out uint host, out uint subnetMask)
        {
            host = 0;
            subnetMask = 0;
            if (AddressFamily != AddressFamily.InterNetwork) return false;
            subnetMask = ~(uint)0 << (32 - PrefixBits);
            if (PrefixBitmask != 0)
                subnetMask |= (uint)(PrefixBitmask << (24 - PrefixBits));
            fixed (byte* pp = _prefix)
                host = BinaryPrimitives.ReadUInt32BigEndian(new Span<byte>(pp, 4));
            return true;
        }
    }
}

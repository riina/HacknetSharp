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
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a CIDR IP range (or a single IP address)
    /// </summary>
    /// <remarks>
    /// Internally, the prefix is stored in a 16-byte buffer purely for having a single struct to use between IPv4 and IPv6
    /// </remarks>
    public unsafe struct IPAddressRange : IEquatable<IPAddressRange>
    {
#pragma warning disable 649
        private fixed byte _prefix[16];
#pragma warning restore 649
        /// <summary>
        /// Address family of this range.
        /// </summary>
        public readonly AddressFamily AddressFamily;

        /// <summary>
        /// Number of prefix bits for this range.
        /// </summary>
        public readonly byte PrefixBits;

        /// <summary>
        /// Creates a new instance of <see cref="IPAddressRange"/> with the provided CIDR range.
        /// </summary>
        /// <param name="cidrString">CIDR range.</param>
        /// <exception cref="ArgumentException">Throws on failure to parse address.</exception>
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

            PrefixBits = (byte)prefixBits;
            int prefixBytes = prefixBits / 8;
            fixed (byte* pp = _prefix)
                addrBytes.Slice(0, prefixBytes).CopyTo(new Span<byte>(pp, 16));
            if (prefixBits % 8 != 0)
                _prefix[prefixBytes] = (byte)((byte)(0xff00 >> prefixBits % 8) & addrBytes[prefixBytes]);
            AddressFamily = address.AddressFamily;
        }

        /// <summary>
        /// Creates a new instance of <see cref="IPAddressRange"/> from the specified <see cref="IPAddress"/>.
        /// </summary>
        /// <param name="address">Address to convert.</param>
        /// <exception cref="ArgumentException">Thrown when failed to convert address.</exception>
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

            PrefixBits = (byte)prefixBits;
            int prefixBytes = prefixBits / 8;
            fixed (byte* pp = _prefix)
                addrBytes.Slice(0, prefixBytes).CopyTo(new Span<byte>(pp, 16));
            if (prefixBits % 8 != 0)
                _prefix[prefixBytes] = (byte)((byte)(0xff00 >> prefixBits % 8) & addrBytes[prefixBytes]);
            AddressFamily = address.AddressFamily;
        }

        private IPAddressRange(Span<byte> prefix, AddressFamily addressFamily, int prefixBits)
        {
            fixed (byte* pp = _prefix) prefix.CopyTo(new Span<byte>(pp, 16));
            AddressFamily = addressFamily;
            PrefixBits = (byte)prefixBits;
        }

        /// <summary>
        /// Attempts to parse a CIDR string to an address range.
        /// </summary>
        /// <param name="cidrString">CIDR range string.</param>
        /// <param name="allowRange">If true, allows ranges instead of just single addresses.</param>
        /// <param name="value">Parsed value if successful.</param>
        /// <returns>Returns true if successfully parsed.</returns>
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
            Span<byte> prefix = stackalloc byte[16];
            addrBytes.Slice(0, prefixBytes).CopyTo(prefix);
            if (prefixBits % 8 != 0)
                prefix[prefixBytes] = (byte)((byte)(0xff00 >> prefixBits % 8) & addrBytes[prefixBytes]);
            var addressFamily = address.AddressFamily;

            value = new IPAddressRange(prefix, addressFamily, prefixBits);
            return true;
        }

        /// <summary>
        /// Checks if address is contained in range.
        /// </summary>
        /// <param name="address">Address to check.</param>
        /// <returns>True if this range contains the specified IP address.</returns>
        /// <exception cref="ArgumentException">Thrown when failed to parse address.</exception>
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
            return PrefixBits % 8 == 0 ||
                   _prefix[prefixBytes] == ((byte)(0xff00 >> PrefixBits % 8) & addrBytes[prefixBytes]);
        }

        /// <summary>
        /// Attempts to get host and subnet components from this range.
        /// </summary>
        /// <param name="host">Host for this range.</param>
        /// <param name="subnetMask">Subnet mask of this range.</param>
        /// <returns>False if not an IPv4 address range.</returns>
        public bool TryGetIPv4HostAndSubnetMask(out uint host, out uint subnetMask)
        {
            host = 0;
            subnetMask = 0;
            if (AddressFamily != AddressFamily.InterNetwork) return false;
            subnetMask = ~(uint)0 << (32 - PrefixBits);
            fixed (byte* pp = _prefix)
                host = BinaryPrimitives.ReadUInt32BigEndian(new Span<byte>(pp, 4));
            return true;
        }

        /// <summary>
        /// Applies host to this address.
        /// </summary>
        /// <param name="host">Host to apply.</param>
        /// <returns>Converted address.</returns>
        /// <exception cref="InvalidOperationException">Thrown if this instance isn't a full 32-bit address or if <paramref name="host"/> isn't an IPv4 address.</exception>
        public IPAddressRange OnHost(IPAddressRange host)
        {
            if (PrefixBits != 32)
                throw new InvalidOperationException("Cannot apply host on IPv4 address without 32 bit prefix");
            if (!TryGetIPv4HostAndSubnetMask(out uint selfHost, out uint _))
                throw new InvalidOperationException("Cannot apply host on non-IPv4 address");
            if (!host.TryGetIPv4HostAndSubnetMask(out uint hostHost, out uint hostSubnetMask))
                throw new InvalidOperationException("Cannot apply non-IPv4 host address");
            uint addr = (~hostSubnetMask & selfHost) | hostHost;
            Span<byte> body = stackalloc byte[4];
            BinaryPrimitives.WriteUInt32BigEndian(body, addr);
            return new IPAddressRange(body, AddressFamily.InterNetwork, 32);
        }

        /// <inheritdoc />
        public bool Equals(IPAddressRange other)
        {
            if (AddressFamily != other.AddressFamily || PrefixBits != other.PrefixBits) return false;
            fixed (byte* sp = _prefix)
                return new Span<byte>(sp, 16).SequenceEqual(new Span<byte>(other._prefix, 16));
        }

        /// <inheritdoc />
        public override bool Equals(object? obj) => obj is IPAddressRange other && Equals(other);

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            Span<byte> prefix;
            fixed (byte* pp = _prefix)
                prefix = new Span<byte>(pp, 16);
            int p1 = BinaryPrimitives.ReadInt32BigEndian(prefix);
            int p2 = BinaryPrimitives.ReadInt32BigEndian(prefix.Slice(4, 4));
            int p3 = BinaryPrimitives.ReadInt32BigEndian(prefix.Slice(8, 4));
            int p4 = BinaryPrimitives.ReadInt32BigEndian(prefix.Slice(12, 4));
            return HashCode.Combine(p1, p2, p3, p4, (int)AddressFamily, PrefixBits);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            fixed (byte* pp = _prefix)
                return AddressFamily switch
                {
                    AddressFamily.InterNetwork => $"{pp[0]}.{pp[1]}.{pp[2]}.{pp[3]}/{PrefixBits}",
                    _ => $"{new IPAddress(new ReadOnlySpan<byte>(pp, 16))}/{PrefixBits}",
                };
        }
    }
}

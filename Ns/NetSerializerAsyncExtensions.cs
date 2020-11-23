using System;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static System.Buffers.ArrayPool<byte>;
using static Ns.NetSerializer;

namespace Ns
{
    /// <summary>
    /// Async extension methods
    /// </summary>
    public static class NetSerializerAsyncExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<T> ReadBase8Async<T>(this Stream stream, CancellationToken cancellationToken)
            where T : struct
        {
            int tot = 0;
            var buffer = Shared.Rent(sizeof(byte));
            try
            {
                do
                {
                    int read = await stream.ReadAsync(buffer, tot, sizeof(byte) - tot, cancellationToken);
                    if (read == 0)
                        throw new EndOfStreamException(
                            $"Failed to read required number of bytes! 0x{tot:X} read, 0x{sizeof(byte) - tot:X} left");
                    tot += read;
                } while (tot < sizeof(byte));

                return MemoryMarshal.Read<T>(buffer);
            }
            finally
            {
                Shared.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<T> ReadBase16Async<T>(this Stream stream, CancellationToken cancellationToken)
            where T : struct
        {
            int tot = 0;
            var buffer = Shared.Rent(sizeof(ushort));
            try
            {
                do
                {
                    int read = await stream.ReadAsync(buffer, tot, sizeof(ushort) - tot, cancellationToken);
                    if (read == 0)
                        throw new EndOfStreamException(
                            $"Failed to read required number of bytes! 0x{tot:X} read, 0x{sizeof(ushort) - tot:X} left");
                    tot += read;
                } while (tot < sizeof(ushort));

                return MemoryMarshal.Read<T>(buffer);
            }
            finally
            {
                Shared.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<T> ReadBase32Async<T>(this Stream stream, CancellationToken cancellationToken)
            where T : struct
        {
            int tot = 0;
            var buffer = Shared.Rent(sizeof(uint));
            try
            {
                do
                {
                    int read = await stream.ReadAsync(buffer, tot, sizeof(uint) - tot, cancellationToken);
                    if (read == 0)
                        throw new EndOfStreamException(
                            $"Failed to read required number of bytes! 0x{tot:X} read, 0x{sizeof(uint) - tot:X} left");
                    tot += read;
                } while (tot < sizeof(uint));

                return MemoryMarshal.Read<T>(buffer);
            }
            finally
            {
                Shared.Return(buffer);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<T> ReadBase64Async<T>(this Stream stream, CancellationToken cancellationToken)
            where T : struct
        {
            int tot = 0;
            var buffer = Shared.Rent(sizeof(ulong));
            try
            {
                do
                {
                    int read = await stream.ReadAsync(buffer, tot, sizeof(ulong) - tot, cancellationToken);
                    if (read == 0)
                        throw new EndOfStreamException(
                            $"Failed to read required number of bytes! 0x{tot:X} read, 0x{sizeof(ulong) - tot:X} left");
                    tot += read;
                } while (tot < sizeof(ulong));

                return MemoryMarshal.Read<T>(buffer);
            }
            finally
            {
                Shared.Return(buffer);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async Task<T> ReadBase128Async<T>(this Stream stream, CancellationToken cancellationToken)
            where T : struct
        {
            int tot = 0;
            var buffer = Shared.Rent(sizeof(decimal));
            try
            {
                do
                {
                    int read = await stream.ReadAsync(buffer, tot, sizeof(decimal) - tot, cancellationToken);
                    if (read == 0)
                        throw new EndOfStreamException(
                            $"Failed to read required number of bytes! 0x{tot:X} read, 0x{sizeof(decimal) - tot:X} left");
                    tot += read;
                } while (tot < sizeof(decimal));

                return MemoryMarshal.Read<T>(buffer);
            }
            finally
            {
                Shared.Return(buffer);
            }
        }

        /// <summary>
        ///     Read signed 8-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Value</returns>
        public static async Task<sbyte> ReadS8Async(this Stream stream, CancellationToken cancellationToken)
        {
            return await ReadBase8Async<sbyte>(stream, cancellationToken);
        }

        /// <summary>
        ///     Read signed 8-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static async Task<byte> ReadU8Async(this Stream stream, CancellationToken cancellationToken)
        {
            return await ReadBase8Async<byte>(stream, cancellationToken);
        }

        /// <summary>
        ///     Read signed 16-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static async Task<short> ReadS16Async(this Stream stream, CancellationToken cancellationToken)
        {
            return _swap
                ? BinaryPrimitives.ReverseEndianness(
                    await ReadBase16Async<short>(stream, cancellationToken))
                : await ReadBase16Async<short>(stream, cancellationToken);
        }

        /// <summary>
        ///     Read unsigned 16-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static async Task<ushort> ReadU16Async(this Stream stream, CancellationToken cancellationToken)
        {
            return _swap
                ? BinaryPrimitives.ReverseEndianness(
                    await ReadBase16Async<ushort>(stream, cancellationToken))
                : await ReadBase16Async<ushort>(stream, cancellationToken);
        }

        /// <summary>
        ///     Read signed 32-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static async Task<int> ReadS32Async(this Stream stream, CancellationToken cancellationToken)
        {
            return _swap
                ? BinaryPrimitives.ReverseEndianness(
                    await ReadBase32Async<int>(stream, cancellationToken))
                : await ReadBase32Async<int>(stream, cancellationToken);
        }

        /// <summary>
        ///     Read unsigned 32-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static async Task<uint> ReadU32Async(this Stream stream, CancellationToken cancellationToken)
        {
            return _swap
                ? BinaryPrimitives.ReverseEndianness(
                    await ReadBase32Async<uint>(stream, cancellationToken))
                : await ReadBase32Async<uint>(stream, cancellationToken);
        }

        /// <summary>
        ///     Read signed 64-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static async Task<long> ReadS64Async(this Stream stream, CancellationToken cancellationToken)
        {
            return _swap
                ? BinaryPrimitives.ReverseEndianness(
                    await ReadBase64Async<long>(stream, cancellationToken))
                : await ReadBase64Async<long>(stream, cancellationToken);
        }

        /// <summary>
        ///     Read unsigned 64-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public static async Task<ulong> ReadU64Async(this Stream stream, CancellationToken cancellationToken)
        {
            return _swap
                ? BinaryPrimitives.ReverseEndianness(
                    await ReadBase64Async<ulong>(stream, cancellationToken))
                : await ReadBase64Async<ulong>(stream, cancellationToken);
        }

        /// <summary>
        ///     Read single-precision floating-point value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Value</returns>
        public static async Task<float> ReadSingleAsync(this Stream stream, CancellationToken cancellationToken)
        {
            return await ReadBase32Async<float>(stream, cancellationToken);
        }

        /// <summary>
        ///     Read double-precision floating-point value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Value</returns>
        public static async Task<double> ReadDoubleAsync(this Stream stream, CancellationToken cancellationToken)
        {
            return await ReadBase64Async<double>(stream, cancellationToken);
        }

        /// <summary>
        ///     Read decimal value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Value</returns>
        public static async Task<decimal> ReadDecimalAsync(this Stream stream, CancellationToken cancellationToken)
        {
            return await ReadBase128Async<decimal>(stream, cancellationToken);
        }

        /// <summary>
        ///     Read Guid value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Value</returns>
        public static async Task<Guid> ReadGuidAsync(this Stream stream, CancellationToken cancellationToken)
        {
            return await ReadBase128Async<Guid>(stream, cancellationToken);
        }
    }
}

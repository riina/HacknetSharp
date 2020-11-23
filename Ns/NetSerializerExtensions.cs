using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static System.Buffers.ArrayPool<byte>;
using static System.Buffers.Binary.BinaryPrimitives;
using static Ns.NetSerializer;

namespace Ns
{
    /// <summary>
    /// Extension methods
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class NetSerializerExtensions
    {
        [ThreadStatic] private static byte[] _buffer;
        [ThreadStatic] private static Decoder _decoder;
        [ThreadStatic] private static Encoder _encoder;
        [ThreadStatic] private static StringBuilder _stringBuilder;
        [ThreadStatic] private static NetSerializer _self;
        private static byte[] Buffer => _buffer ??= new byte[sizeof(decimal)];
        private static Decoder Decoder => _decoder ??= Encoding.UTF8.GetDecoder();
        private static Encoder Encoder => _encoder ??= Encoding.UTF8.GetEncoder();
        private static StringBuilder StringBuilder => _stringBuilder ??= new StringBuilder();
        private static NetSerializer Self => _self ??= new NetSerializer(Stream.Null);

        /// <summary>
        ///     Read array
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="count">Number of elements</param>
        /// <param name="enableSwap">Enable element endianness swapping</param>
        /// <param name="target">Optional existing array to operate on</param>
        /// <typeparam name="T">Type of elements</typeparam>
        /// <returns>Array</returns>
        /// <exception cref="ApplicationException">If failed to read required number of bytes</exception>
        public static T[] ReadArray<T>(this Stream stream, int count, bool enableSwap, T[] target = null)
            where T : unmanaged
        {
            target ??= new T[count];
            ReadSpan<T>(stream, target, count, enableSwap);
            return target;
        }

        /// <summary>
        ///     Read list
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="count">Number of elements</param>
        /// <param name="enableSwap">Enable element endianness swapping</param>
        /// <param name="target">Optional existing array to operate on</param>
        /// <typeparam name="T">Type of elements</typeparam>
        /// <returns>Array</returns>
        /// <exception cref="ApplicationException">If failed to read required number of bytes</exception>
        public static unsafe List<T> ReadList<T>(this Stream stream, int count, bool enableSwap, List<T> target = null)
            where T : unmanaged
        {
            target ??= new List<T>();
            if (count == 0)
                return target;
            if (target.Capacity < count)
                target.Capacity = count;
            int order = sizeof(T);
            int mainLen = count * order;
            var buf = Shared.Rent(4096);
            var span = buf.AsSpan();
            try
            {
                fixed (byte* p = &span.GetPinnableReference())
                {
                    int left = mainLen, read, tot = 0, curTot = 0;
                    if (order == 1 || !enableSwap || !_swap)
                    {
                        do
                        {
                            read = stream.Read(buf, curTot, Math.Min(4096 - curTot, left));
                            if (read == 0)
                                throw new ApplicationException(
                                    $"Failed to read required number of bytes! 0x{tot:X} read, 0x{left:X} left");
                            curTot += read;
                            int trunc = curTot - curTot % order;
                            for (int i = 0; i < trunc; i += order)
                                target.Add(*(T*)(p + i));
                            curTot -= trunc;
                            span.Slice(trunc, curTot).CopyTo(span);
                            left -= read;
                            tot += read;
                        } while (left > 0);

                        return target;
                    }

                    do
                    {
                        read = stream.Read(buf, curTot, Math.Min(4096 - curTot, left));
                        if (read == 0)
                            throw new ApplicationException(
                                $"Failed to read required number of bytes! 0x{tot:X} read, 0x{left:X} left");
                        curTot += read;
                        int trunc = curTot - curTot % order;
                        switch (order)
                        {
                            case 2:
                                var tmp2 = (short*)p;
                                for (int i = 0; i < trunc; i += 2)
                                {
                                    *tmp2 = ReverseEndianness(*tmp2);
                                    target.Add(*(T*)tmp2);
                                    tmp2++;
                                }

                                break;
                            case 4:
                                var tmp4 = (int*)p;
                                for (int i = 0; i < trunc; i += 4)
                                {
                                    *tmp4 = ReverseEndianness(*tmp4);
                                    target.Add(*(T*)tmp4);
                                    tmp4++;
                                }

                                break;
                            case 8:
                                var tmp8 = (long*)p;
                                for (int i = 0; i < trunc; i += 8)
                                {
                                    *tmp8 = ReverseEndianness(*tmp8);
                                    target.Add(*(T*)tmp8);
                                    tmp8++;
                                }

                                break;
                            default:
                                int half = order / 2;
                                for (int i = 0; i < trunc; i += order)
                                {
                                    for (int j = 0; j < half; j++)
                                    {
                                        int fir = i + j;
                                        int sec = i + order - 1 - j;
                                        byte tmp = p[fir];
                                        p[fir] = p[sec];
                                        p[sec] = tmp;
                                    }

                                    target.Add(*(T*)(p + i));
                                }

                                break;
                        }

                        curTot -= trunc;
                        span.Slice(trunc, curTot).CopyTo(span);
                        left -= read;
                        tot += read;
                    } while (left > 0);

                    return target;
                }
            }
            finally
            {
                Shared.Return(buf);
            }
        }

        /// <summary>
        ///     Read span
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="target">Target buffer</param>
        /// <param name="count">Number of elements</param>
        /// <param name="enableSwap">Enable element endianness swapping</param>
        /// <typeparam name="T">Type of elements</typeparam>
        /// <exception cref="ApplicationException">If failed to read required number of bytes</exception>
        public static unsafe void ReadSpan<T>(this Stream stream, Span<T> target, int count, bool enableSwap)
            where T : unmanaged
        {
            if (count == 0) return;
            var mainTarget = MemoryMarshal.Cast<T, byte>(target);
            int order = sizeof(T);
            int mainLen = count * order;
            var buf = Shared.Rent(4096);
            var span = buf.AsSpan();
            try
            {
                int left = mainLen, tot = 0;
                do
                {
                    int read = stream.Read(buf, 0, Math.Min(4096, left));
                    if (read == 0)
                        throw new ApplicationException(
                            $"Failed to read required number of bytes! 0x{tot:X} read, 0x{left:X} left");
                    span.Slice(0, read).CopyTo(mainTarget.Slice(tot));
                    left -= read;
                    tot += read;
                } while (left > 0);

                if (order != 1 && enableSwap && _swap)
                    fixed (byte* p = &mainTarget.GetPinnableReference())
                    {
                        switch (order)
                        {
                            case 2:
                                var tmp2 = (short*)p;
                                for (int i = 0; i < mainLen; i += 2)
                                {
                                    *tmp2 = ReverseEndianness(*tmp2);
                                    tmp2++;
                                }

                                break;
                            case 4:
                                var tmp4 = (int*)p;
                                for (int i = 0; i < mainLen; i += 4)
                                {
                                    *tmp4 = ReverseEndianness(*tmp4);
                                    tmp4++;
                                }

                                break;
                            case 8:
                                var tmp8 = (long*)p;
                                for (int i = 0; i < mainLen; i += 8)
                                {
                                    *tmp8 = ReverseEndianness(*tmp8);
                                    tmp8++;
                                }

                                break;
                            default:
                                int half = order / 2;
                                for (int i = 0; i < mainLen; i += order)
                                for (int j = 0; j < half; j++)
                                {
                                    int fir = i + j;
                                    int sec = i + order - 1 - j;
                                    byte tmp = p[fir];
                                    p[fir] = p[sec];
                                    p[sec] = tmp;
                                }

                                break;
                        }
                    }
            }
            finally
            {
                Shared.Return(buf);
            }
        }

        /// <summary>
        ///     Write span
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="source">Source buffer</param>
        /// <param name="count">Number of elements</param>
        /// <param name="enableSwap">Enable element endianness swapping</param>
        /// <typeparam name="T">Type of elements</typeparam>
        public static unsafe void WriteSpan<T>(this Stream stream, Span<T> source, int count, bool enableSwap)
            where T : unmanaged
        {
            if (count == 0)
                return;
            var mainTarget = MemoryMarshal.Cast<T, byte>(source);
            var buf = Shared.Rent(4096);
            var span = buf.AsSpan();
            int order = sizeof(T);
            int left = count * order;
            int tot = 0;
            try
            {
                if (order == 1 || !enableSwap || !_swap)
                {
                    while (left > 0)
                    {
                        int noSwapCur = Math.Min(left, 4096);
                        mainTarget.Slice(tot, noSwapCur).CopyTo(buf);
                        stream.Write(buf, 0, noSwapCur);
                        left -= noSwapCur;
                        tot += noSwapCur;
                    }

                    return;
                }

                int maxCount = 4096 / order * order;
                fixed (byte* p = &span.GetPinnableReference())
                {
                    while (left != 0)
                    {
                        int cur = Math.Min(left, maxCount);
                        mainTarget.Slice(tot, cur).CopyTo(buf);
                        switch (order)
                        {
                            case 2:
                                var tmp2 = (short*)p;
                                for (int i = 0; i < cur; i += 2)
                                {
                                    *tmp2 = ReverseEndianness(*tmp2);
                                    tmp2++;
                                }

                                break;
                            case 4:
                                var tmp4 = (int*)p;
                                for (int i = 0; i < cur; i += 4)
                                {
                                    *tmp4 = ReverseEndianness(*tmp4);
                                    tmp4++;
                                }

                                break;
                            case 8:
                                var tmp8 = (long*)p;
                                for (int i = 0; i < cur; i += 8)
                                {
                                    *tmp8 = ReverseEndianness(*tmp8);
                                    tmp8++;
                                }

                                break;
                            default:
                                int half = order / 2;
                                for (int i = 0; i < cur; i += order)
                                for (int j = 0; j < half; j++)
                                {
                                    int fir = i + j;
                                    int sec = i + order - 1 - j;
                                    byte tmp = p[fir];
                                    p[fir] = p[sec];
                                    p[sec] = tmp;
                                }

                                break;
                        }

                        stream.Write(buf, 0, cur);
                        left -= cur;
                        tot += cur;
                    }
                }
            }
            finally
            {
                Shared.Return(buf);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<byte> ReadBase16(this Stream stream)
        {
            int tot = 0;
            do
            {
                int read = stream.Read(Buffer, tot, sizeof(ushort) - tot);
                if (read == 0)
                    throw new EndOfStreamException(
                        $"Failed to read required number of bytes! 0x{tot:X} read, 0x{sizeof(ushort) - tot:X} left");
                tot += read;
            } while (tot < sizeof(ushort));

            return Buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<byte> ReadBase32(this Stream stream)
        {
            int tot = 0;
            do
            {
                int read = stream.Read(Buffer, tot, sizeof(uint) - tot);
                if (read == 0)
                    throw new EndOfStreamException(
                        $"Failed to read required number of bytes! 0x{tot:X} read, 0x{sizeof(uint) - tot:X} left");
                tot += read;
            } while (tot < sizeof(uint));

            return Buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<byte> ReadBase64(this Stream stream)
        {
            int tot = 0;
            do
            {
                int read = stream.Read(Buffer, tot, sizeof(ulong) - tot);
                if (read == 0)
                    throw new EndOfStreamException(
                        $"Failed to read required number of bytes! 0x{tot:X} read, 0x{sizeof(ulong) - tot:X} left");
                tot += read;
            } while (tot < sizeof(ulong));

            return Buffer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Span<byte> ReadBase128(this Stream stream)
        {
            int tot = 0;
            do
            {
                int read = stream.Read(Buffer, tot, sizeof(decimal) - tot);
                if (read == 0)
                    throw new EndOfStreamException(
                        $"Failed to read required number of bytes! 0x{tot:X} read, 0x{sizeof(decimal) - tot:X} left");
                tot += read;
            } while (tot < sizeof(decimal));

            return Buffer;
        }

        /// <summary>
        ///     Read UTF-8 string
        /// </summary>
        /// <returns>Decoded string</returns>
        public static unsafe string ReadUtf8String(this Stream stream)
        {
            Decoder.Reset();
            var tmpBuf = Shared.Rent(4096);
            try
            {
                fixed (byte* tmpBufPtr = tmpBuf)
                {
                    var charPtr = (char*)(tmpBufPtr + 2048);
                    int len;
                    while ((len = Read7S32(stream, out _)) != 0)
                    {
                        int tot = 0;
                        do
                        {
                            int read = stream.Read(tmpBuf, 0, Math.Min(len, 2048));
                            if (read == 0)
                                throw new ApplicationException(
                                    $"Failed to read required number of bytes! 0x{tot:X} read, 0x{len:X} left");
                            len -= read;
                            int cur = 0;
                            do
                            {
                                Decoder.Convert(tmpBufPtr + cur, read - cur, charPtr, 2048 / sizeof(char),
                                    false, out int numIn, out int numOut, out _);
                                StringBuilder.Append(charPtr, numOut);
                                cur += numIn;
                            } while (cur != read);

                            tot += read;
                        } while (len > 0);
                    }
                }

                string str = StringBuilder.ToString();
                StringBuilder.Clear();
                return str;
            }
            finally
            {
                Shared.Return(tmpBuf);
            }
        }

        /// <summary>
        ///     Write UTF-8 string
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">String to write</param>
        public static unsafe void WriteUtf8String(this Stream stream, string value)
        {
            Encoder.Reset();
            var tmpBuf = Shared.Rent(4096);
            try
            {
                fixed (char* strPtr = value)
                {
                    fixed (byte* tmpBufPtr = tmpBuf)
                    {
                        int vStringOfs = 0;
                        int vStringLeft = value.Length;
                        while (vStringLeft > 0)
                        {
                            Encoder.Convert(strPtr + vStringOfs, vStringLeft, tmpBufPtr, 4096, false,
                                out int numIn, out int numOut, out _);
                            vStringOfs += numIn;
                            vStringLeft -= numIn;
                            Write7S32(stream, numOut);
                            stream.Write(tmpBuf, 0, numOut);
                        }
                    }
                }
            }
            finally
            {
                Shared.Return(tmpBuf);
            }

            Write7S32(stream, 0);
        }

        /// <summary>
        ///     Decode 32-bit integer
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="len">Length of decoded value</param>
        /// <returns>Decoded value</returns>
        /// <exception cref="EndOfStreamException">When end of stream was prematurely reached</exception>
        public static int Read7S32(this Stream stream, out int len)
        {
            len = 1;
            int bits = 6;
            int c = stream.Read(Buffer, 0, 1);
            if (c == 0)
                throw new EndOfStreamException();
            byte v = Buffer[0];
            int value = v & 0x3f;
            bool flag = (v & 0x40) != 0;
            if (v <= 0x7f)
                while (bits < sizeof(int) * 8)
                {
                    int c2 = stream.Read(Buffer, 0, 1);
                    if (c2 == 0)
                        throw new EndOfStreamException();
                    byte v2 = Buffer[0];
                    value |= (v2 & 0x7f) << bits;
                    len++;
                    bits += 7;
                    if (v2 > 0x7f)
                        break;
                }

            if (flag)
                value = ~value;

            return value;
        }

        /// <summary>
        ///     Encode 32-bit integer
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value to encode</param>
        /// <returns>Length of encoded value</returns>
        public static int Write7S32(this Stream stream, int value)
        {
            byte flag = 0;
            if (value < 0)
            {
                value = ~value;
                flag = 0x40;
            }

            uint uValue = (uint)value;
            int len = 1;
            int ofs = 0;
            if (uValue < 0x40)
                Buffer[ofs++] = (byte)(0x80 | (uValue & 0x3f) | flag);
            else
                Buffer[ofs++] = (byte)((uValue & 0x3f) | flag);
            uValue >>= 6;
            while (uValue != 0)
            {
                if (uValue < 0x80)
                    Buffer[ofs++] = (byte)(0x80 | (uValue & 0x7f));
                else
                    Buffer[ofs++] = (byte)(uValue & 0x7f);
                len++;
                uValue >>= 7;
            }

            stream.Write(Buffer, 0, ofs);

            return len;
        }

        /// <summary>
        ///     Decode 64-bit integer
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="len">Length of decoded value</param>
        /// <returns>Decoded value</returns>
        /// <exception cref="EndOfStreamException">When end of stream was prematurely reached</exception>
        public static long Read7S64(this Stream stream, out int len)
        {
            len = 1;
            int bits = 6;
            int c = stream.Read(Buffer, 0, 1);
            if (c == 0)
                throw new EndOfStreamException();
            byte v = Buffer[0];
            long value = v & 0x3f;
            bool flag = (v & 0x40) != 0;
            if (v <= 0x7f)
                while (bits < sizeof(long) * 8)
                {
                    int c2 = stream.Read(Buffer, 0, 1);
                    if (c2 == 0)
                        throw new EndOfStreamException();
                    byte v2 = Buffer[0];
                    value |= (long)(v2 & 0x7f) << bits;
                    len++;
                    bits += 7;
                    if (v2 > 0x7f)
                        break;
                }

            if (flag)
                value = ~value;

            return value;
        }

        /// <summary>
        ///     Encode 64-bit integer
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value to encode</param>
        /// <returns>Length of encoded value</returns>
        public static int Write7S64(this Stream stream, long value)
        {
            byte flag = 0;
            if (value < 0)
            {
                value = ~value;
                flag = 0x40;
            }

            ulong uValue = (ulong)value;
            int len = 1;
            int ofs = 0;
            if (uValue < 0x40)
                Buffer[ofs++] = (byte)(0x80 | (uValue & 0x3f) | flag);
            else
                Buffer[ofs++] = (byte)((uValue & 0x3f) | flag);
            uValue >>= 6;
            while (uValue != 0)
            {
                if (uValue < 0x80)
                    Buffer[ofs++] = (byte)(0x80 | (uValue & 0x7f));
                else
                    Buffer[ofs++] = (byte)(uValue & 0x7f);
                len++;
                uValue >>= 7;
            }

            stream.Write(Buffer, 0, ofs);

            return len;
        }

        /// <summary>
        ///     Read signed 8-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <returns>Value</returns>
        public static sbyte ReadS8(this Stream stream) =>
            stream.Read(Buffer, 0, 1) == 0
                ? throw new ApplicationException(
                    "Failed to read required number of bytes! 0x0 read, 0x1 left")
                : (sbyte)Buffer[0];

        /// <summary>
        ///     Write signed 8-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value</param>
        public static void WriteS8(this Stream stream, sbyte value)
        {
            Buffer[0] = (byte)value;
            stream.Write(Buffer, 0, sizeof(byte));
        }

        /// <summary>
        ///     Read unsigned 8-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <returns>Value</returns>
        public static byte ReadU8(this Stream stream) =>
            stream.Read(Buffer, 0, 1) == 0
                ? throw new ApplicationException(
                    "Failed to read required number of bytes! 0x0 read, 0x1 left")
                : Buffer[0];

        /// <summary>
        ///     Write unsigned 8-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value</param>
        public static void WriteU8(this Stream stream, byte value)
        {
            Buffer[0] = value;
            stream.Write(Buffer, 0, sizeof(byte));
        }

        /// <summary>
        ///     Read signed 16-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <returns>Value</returns>
        public static short ReadS16(this Stream stream)
        {
            return _swap
                ? ReverseEndianness(
                    MemoryMarshal.Read<short>(ReadBase16(stream)))
                : MemoryMarshal.Read<short>(ReadBase16(stream));
        }

        /// <summary>
        ///     Write signed 16-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value</param>
        public static void WriteS16(this Stream stream, short value)
        {
            if (_swap)
                value = ReverseEndianness(value);
            MemoryMarshal.Write(Buffer, ref value);
            stream.Write(Buffer, 0, sizeof(short));
        }

        /// <summary>
        ///     Read unsigned 16-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <returns>Value</returns>
        public static ushort ReadU16(this Stream stream)
        {
            return _swap
                ? ReverseEndianness(
                    MemoryMarshal.Read<ushort>(ReadBase16(stream)))
                : MemoryMarshal.Read<ushort>(ReadBase16(stream));
        }

        /// <summary>
        ///     Write unsigned 16-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value</param>
        public static void WriteU16(this Stream stream, ushort value)
        {
            if (_swap)
                value = ReverseEndianness(value);
            MemoryMarshal.Write(Buffer, ref value);
            stream.Write(Buffer, 0, sizeof(ushort));
        }

        /// <summary>
        ///     Read signed 32-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <returns>Value</returns>
        public static int ReadS32(this Stream stream)
        {
            return _swap
                ? ReverseEndianness(
                    MemoryMarshal.Read<int>(ReadBase32(stream)))
                : MemoryMarshal.Read<int>(ReadBase32(stream));
        }

        /// <summary>
        ///     Write signed 32-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value</param>
        public static void WriteS32(this Stream stream, int value)
        {
            if (_swap)
                value = ReverseEndianness(value);
            MemoryMarshal.Write(Buffer, ref value);
            stream.Write(Buffer, 0, sizeof(int));
        }

        /// <summary>
        ///     Read unsigned 32-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <returns>Value</returns>
        public static uint ReadU32(this Stream stream)
        {
            return _swap
                ? ReverseEndianness(
                    MemoryMarshal.Read<uint>(ReadBase32(stream)))
                : MemoryMarshal.Read<uint>(ReadBase32(stream));
        }

        /// <summary>
        ///     Write unsigned 32-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value</param>
        public static void WriteU32(this Stream stream, uint value)
        {
            if (_swap)
                value = ReverseEndianness(value);
            MemoryMarshal.Write(Buffer, ref value);
            stream.Write(Buffer, 0, sizeof(uint));
        }

        /// <summary>
        ///     Read signed 64-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <returns>Value</returns>
        public static long ReadS64(this Stream stream)
        {
            return _swap
                ? ReverseEndianness(
                    MemoryMarshal.Read<long>(ReadBase64(stream)))
                : MemoryMarshal.Read<long>(ReadBase64(stream));
        }

        /// <summary>
        ///     Write signed 64-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value</param>
        public static void WriteS64(this Stream stream, long value)
        {
            if (_swap)
                value = ReverseEndianness(value);
            MemoryMarshal.Write(Buffer, ref value);
            stream.Write(Buffer, 0, sizeof(long));
        }

        /// <summary>
        ///     Read unsigned 64-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <returns>Value</returns>
        public static ulong ReadU64(this Stream stream)
        {
            return _swap
                ? ReverseEndianness(
                    MemoryMarshal.Read<ulong>(ReadBase64(stream)))
                : MemoryMarshal.Read<ulong>(ReadBase64(stream));
        }

        /// <summary>
        ///     Write unsigned 64-bit value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value</param>
        public static void WriteU64(this Stream stream, ulong value)
        {
            if (_swap)
                value = ReverseEndianness(value);
            MemoryMarshal.Write(Buffer, ref value);
            stream.Write(Buffer, 0, sizeof(ulong));
        }

        /// <summary>
        ///     Read single-precision floating-point value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <returns>Value</returns>
        public static float ReadSingle(this Stream stream)
        {
            return MemoryMarshal.Read<float>(ReadBase32(stream));
        }

        /// <summary>
        ///     Write single-precision floating-point value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value</param>
        public static void WriteSingle(this Stream stream, float value)
        {
            MemoryMarshal.Write(Buffer, ref value);
            stream.Write(Buffer, 0, sizeof(float));
        }

        /// <summary>
        ///     Read double-precision floating-point value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <returns>Value</returns>
        public static double ReadDouble(this Stream stream)
        {
            return MemoryMarshal.Read<double>(ReadBase64(stream));
        }

        /// <summary>
        ///     Write double-precision floating-point value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value</param>
        public static void WriteDouble(this Stream stream, double value)
        {
            MemoryMarshal.Write(Buffer, ref value);
            stream.Write(Buffer, 0, sizeof(double));
        }

        /// <summary>
        ///     Read decimal value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <returns>Value</returns>
        public static decimal ReadDecimal(this Stream stream)
        {
            return MemoryMarshal.Read<decimal>(ReadBase128(stream));
        }

        /// <summary>
        ///     Write decimal value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value</param>
        public static void WriteDecimal(this Stream stream, decimal value)
        {
            MemoryMarshal.Write(Buffer, ref value);
            stream.Write(Buffer, 0, sizeof(decimal));
        }

        /// <summary>
        ///     Read Guid value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <returns>Value</returns>
        public static Guid ReadGuid(this Stream stream)
        {
            return MemoryMarshal.Read<Guid>(ReadBase128(stream));
        }

        /// <summary>
        ///     Write Guid value
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="value">Value</param>
        public static void WriteGuid(this Stream stream, Guid value)
        {
            MemoryMarshal.Write(Buffer, ref value);
            stream.Write(Buffer, 0, 16);
        }

        /// <summary>
        ///     Serialize an object
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="obj">Object to serialize</param>
        /// <typeparam name="T">Type of object to be serialized</typeparam>
        /// <exception cref="ApplicationException">If attempting to serialize unregistered type</exception>
        public static void Serialize<T>(this Stream stream, T obj)
        {
            if (!GetConverter<T>(out var res))
                throw new ApplicationException("Tried to serialize unregistered type");
            stream.Serialize(obj, !typeof(T).IsValueType, res.encoder);
        }

        /// <summary>
        ///     Serialize an object
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="obj">Object to serialize</param>
        /// <param name="nullCheck">Serialize null state</param>
        /// <typeparam name="T">Type of object to be serialized</typeparam>
        /// <exception cref="ApplicationException">If attempting to serialize unregistered type</exception>
        public static void Serialize<T>(this Stream stream, T obj, bool nullCheck)
        {
            if (!GetConverter<T>(out var res))
                throw new ApplicationException("Tried to serialize unregistered type");
            stream.Serialize(obj, nullCheck, res.encoder);
        }

        /// <summary>
        ///     Serialize an object
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="obj">Object to serialize</param>
        /// <param name="nullCheck">Serialize null state</param>
        /// <param name="encoder">Encoder to use</param>
        /// <typeparam name="T">Type of object to be serialized</typeparam>
        /// <exception cref="ApplicationException">If attempting to serialize unregistered type</exception>
        public static void Serialize<T>(this Stream stream, T obj, bool nullCheck,
            Action<NetSerializer, Stream, T> encoder)
        {
            if (nullCheck)
            {
                stream.WriteU8((byte)(obj != null ? 1 : 0));
                if (obj == null) return;
            }

            encoder.Invoke(Self, stream, obj);
        }

        /// <summary>
        ///     Deserialize an object
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <typeparam name="T">Type of object to be deserialized</typeparam>
        /// <exception cref="ApplicationException">If attempting to serialize unregistered type</exception>
        /// <returns>Deserialized object or null</returns>
        public static T Deserialize<T>(this Stream stream)
        {
            if (!GetConverter<T>(out var res))
                throw new ApplicationException("Tried to deserialize unregistered type");
            return stream.Deserialize(!typeof(T).IsValueType, res.decoder);
        }

        /// <summary>
        ///     Deserialize an object
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="nullCheck">Deserialize null state</param>
        /// <typeparam name="T">Type of object to be deserialized</typeparam>
        /// <returns>Deserialized object or null</returns>
        public static T Deserialize<T>(this Stream stream, bool nullCheck)
        {
            if (!GetConverter<T>(out var res))
                throw new ApplicationException("Tried to deserialize unregistered type");
            return stream.Deserialize(nullCheck, res.decoder);
        }

        /// <summary>
        ///     Deserialize an object
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="nullCheck">Deserialize null state</param>
        /// <param name="decoder">Decoder to use</param>
        /// <typeparam name="T">Type of object to be deserialized</typeparam>
        /// <returns>Deserialized object or null</returns>
        public static T Deserialize<T>(this Stream stream, bool nullCheck, Func<NetSerializer, Stream, T> decoder)
        {
            if (nullCheck && stream.ReadU8() == 0) return default;
            return decoder.Invoke(Self, stream);
        }
    }
}

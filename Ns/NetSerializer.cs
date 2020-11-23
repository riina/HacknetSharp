using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Ns
{
    /// <summary>
    ///     Manages serialization to / deserialization from a stream
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
    public class NetSerializer
    {
        internal static readonly bool _swap = !BitConverter.IsLittleEndian;

        private static readonly Dictionary<Type, (object encoder, object decoder)> _converters =
            new Dictionary<Type, (object encoder, object decoder)>();

        static NetSerializer()
        {
            #region Register primitives

            #region Base primitives

            RegisterInternal((s, stream, o) => stream.WriteU8((byte)(o ? 1 : 0)), (s, stream) => stream.ReadU8() != 0);
            RegisterInternal((s, stream, o) => stream.WriteU8(o), (s, stream) => stream.ReadU8());
            RegisterInternal((s, stream, o) => stream.WriteS8(o), (s, stream) => stream.ReadS8());
            RegisterInternal((s, stream, o) => stream.WriteU16(o), (s, stream) => stream.ReadU16());
            RegisterInternal((s, stream, o) => stream.WriteS16(o), (s, stream) => stream.ReadS16());
            RegisterInternal((s, stream, o) => stream.WriteU32(o), (s, stream) => stream.ReadU32());
            RegisterInternal((s, stream, o) => stream.WriteS32(o), (s, stream) => stream.ReadS32());
            RegisterInternal((s, stream, o) => stream.WriteU64(o), (s, stream) => stream.ReadU64());
            RegisterInternal((s, stream, o) => stream.WriteS64(o), (s, stream) => stream.ReadS64());
            RegisterInternal((s, stream, o) => stream.WriteSingle(o), (s, stream) => stream.ReadSingle());
            RegisterInternal((s, stream, o) => stream.WriteDouble(o), (s, stream) => stream.ReadDouble());
            RegisterInternal((s, stream, o) => stream.WriteDecimal(o), (s, stream) => stream.ReadDecimal());
            RegisterInternal((s, stream, o) => stream.WriteU16(o), (s, stream) => (char)stream.ReadU16());
            RegisterInternal((s, stream, o) => stream.WriteGuid(o), (s, stream) => stream.ReadGuid());

            #endregion

            #region Arrays

            RegisterInternal(MakeNativeArrayEncoder<bool>(false), MakeNativeArrayDecoder<bool>(false));
            RegisterInternal(MakeNativeArrayEncoder<byte>(false), MakeNativeArrayDecoder<byte>(false));
            RegisterInternal(MakeNativeArrayEncoder<sbyte>(false), MakeNativeArrayDecoder<sbyte>(false));
            RegisterInternal(MakeNativeArrayEncoder<ushort>(true), MakeNativeArrayDecoder<ushort>(true));
            RegisterInternal(MakeNativeArrayEncoder<short>(true), MakeNativeArrayDecoder<short>(true));
            RegisterInternal(MakeNativeArrayEncoder<uint>(true), MakeNativeArrayDecoder<uint>(true));
            RegisterInternal(MakeNativeArrayEncoder<int>(true), MakeNativeArrayDecoder<int>(true));
            RegisterInternal(MakeNativeArrayEncoder<ulong>(true), MakeNativeArrayDecoder<ulong>(true));
            RegisterInternal(MakeNativeArrayEncoder<long>(true), MakeNativeArrayDecoder<long>(true));
            RegisterInternal(MakeNativeArrayEncoder<float>(false), MakeNativeArrayDecoder<float>(false));
            RegisterInternal(MakeNativeArrayEncoder<double>(false), MakeNativeArrayDecoder<double>(false));
            RegisterInternal(MakeNativeArrayEncoder<char>(true), MakeNativeArrayDecoder<char>(true));
            RegisterInternal(MakeNativeArrayEncoder<decimal>(false), MakeNativeArrayDecoder<decimal>(false));
            RegisterInternal(MakeNativeArrayEncoder<Guid>(false), MakeNativeArrayDecoder<Guid>(false));

            #endregion

            #region Lists

            RegisterInternal((s, stream, bList) =>
                {
                    foreach (bool b in bList) stream.WriteU8((byte)(b ? 1 : 0));
                },
                (s, stream) => stream.ReadList<bool>(stream.ReadS32(), false));

            RegisterInternal((s, stream, u8List) =>
                {
                    stream.WriteS32(u8List.Count);
                    foreach (byte u8 in u8List) stream.WriteU8(u8);
                },
                (s, stream) => stream.ReadList<byte>(stream.ReadS32(), false));

            RegisterInternal((s, stream, s8List) =>
                {
                    stream.WriteS32(s8List.Count);
                    foreach (sbyte s8 in s8List) stream.WriteS8(s8);
                },
                (s, stream) => stream.ReadList<sbyte>(stream.ReadS32(), false));

            RegisterInternal((s, stream, u16List) =>
                {
                    stream.WriteS32(u16List.Count);
                    foreach (ushort u16 in u16List) stream.WriteU16(u16);
                },
                (s, stream) => stream.ReadList<ushort>(stream.ReadS32(), true));

            RegisterInternal((s, stream, s16List) =>
                {
                    stream.WriteS32(s16List.Count);
                    foreach (short s16 in s16List) stream.WriteS16(s16);
                },
                (s, stream) => stream.ReadList<short>(stream.ReadS32(), true));

            RegisterInternal((s, stream, u32List) =>
                {
                    stream.WriteS32(u32List.Count);
                    foreach (uint u32 in u32List) stream.WriteU32(u32);
                },
                (s, stream) => stream.ReadList<uint>(stream.ReadS32(), true));

            RegisterInternal((s, stream, s32List) =>
                {
                    stream.WriteS32(s32List.Count);
                    foreach (int s32 in s32List) stream.WriteS32(s32);
                },
                (s, stream) => stream.ReadList<int>(stream.ReadS32(), true));

            RegisterInternal((s, stream, u64List) =>
                {
                    stream.WriteS32(u64List.Count);
                    foreach (ulong u64 in u64List) stream.WriteU64(u64);
                },
                (s, stream) => stream.ReadList<ulong>(stream.ReadS32(), true));

            RegisterInternal((s, stream, s64List) =>
                {
                    stream.WriteS32(s64List.Count);
                    foreach (long s64 in s64List) stream.WriteS64(s64);
                },
                (s, stream) => stream.ReadList<long>(stream.ReadS32(), true));

            RegisterInternal((s, stream, fList) =>
                {
                    stream.WriteS32(fList.Count);
                    foreach (float f in fList) stream.WriteSingle(f);
                },
                (s, stream) => stream.ReadList<float>(stream.ReadS32(), false));

            RegisterInternal((s, stream, dList) =>
                {
                    stream.WriteS32(dList.Count);
                    foreach (double d in dList) stream.WriteDouble(d);
                },
                (s, stream) => stream.ReadList<double>(stream.ReadS32(), false));

            RegisterInternal((s, stream, deList) =>
                {
                    stream.WriteS32(deList.Count);
                    foreach (decimal de in deList) stream.WriteDecimal(de);
                },
                (s, stream) => stream.ReadList<decimal>(stream.ReadS32(), false));

            RegisterInternal((s, stream, cList) =>
                {
                    stream.WriteS32(cList.Count);
                    foreach (char c in cList) stream.WriteU16(c);
                },
                (s, stream) => stream.ReadList<char>(stream.ReadS32(), false));

            RegisterInternal((s, stream, guList) =>
                {
                    stream.WriteS32(guList.Count);
                    foreach (var gu in guList) stream.WriteGuid(gu);
                },
                (s, stream) => stream.ReadList<Guid>(stream.ReadS32(), false));

            #endregion

            #endregion

            #region Register strings

            RegisterInternal((s, stream, o) => stream.WriteUtf8String(o),
                (s, stream) => stream.ReadUtf8String());

            RegisterInternal((s, stream, strArr) =>
                {
                    stream.WriteS32(strArr.Length);
                    foreach (string str in strArr)
                    {
                        stream.WriteU8((byte)(str != null ? 1 : 0));
                        if (str != null)
                            stream.WriteUtf8String(str);
                    }
                },
                (s, stream) =>
                {
                    int count = stream.ReadS32();
                    var arr = new string[count];
                    for (int i = 0; i < count; i++) arr[i] = stream.ReadU8() == 0 ? null : stream.ReadUtf8String();

                    return arr;
                });

            RegisterInternal((s, stream, strList) =>
                {
                    stream.WriteS32(strList.Count);
                    foreach (string str in strList)
                    {
                        stream.WriteU8((byte)(str != null ? 1 : 0));
                        if (str != null)
                            stream.WriteUtf8String(str);
                    }
                },
                (s, stream) =>
                {
                    int count = stream.ReadS32();
                    var list = new List<string>(count);
                    for (int i = 0; i < count; i++) list.Add(stream.ReadU8() == 0 ? null : stream.ReadUtf8String());

                    return list;
                });

            #endregion

            #region Register misc collections

            AddDictionary<string, string>();
            AddDictionary<string, byte>();
            AddDictionary<string, sbyte>();
            AddDictionary<string, ushort>();
            AddDictionary<string, short>();
            AddDictionary<string, uint>();
            AddDictionary<string, int>();
            AddDictionary<string, ulong>();
            AddDictionary<string, long>();
            AddDictionary<string, float>();
            AddDictionary<string, double>();
            AddDictionary<string, decimal>();
            AddDictionary<string, char>();
            AddDictionary<string, Guid>();

            #endregion
        }

        /// <summary>
        ///     Create new NetSerializer instance
        /// </summary>
        /// <param name="baseStream">Stream to wrap</param>
        /// <param name="converters">Custom converters to use</param>
        public NetSerializer(Stream baseStream,
            IReadOnlyDictionary<Type, (object encoder, object decoder)> converters = null)
        {
            BaseStream = baseStream;
            CustomConverters = converters;
        }

        /// <summary>
        ///     Stream this instance wraps
        /// </summary>
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public Stream BaseStream { get; set; }

        /// <summary>
        ///     Converters specific to this serializer
        /// </summary>
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public IReadOnlyDictionary<Type, (object encoder, object decoder)> CustomConverters { get; set; }


        private static void RegisterInternal<T>(Action<NetSerializer, Stream, T> encoder,
            Func<NetSerializer, Stream, T> decoder)
        {
            _converters[typeof(T)] = (encoder, decoder);
        }

        private static Action<NetSerializer, Stream, T[]> MakeNativeArrayEncoder<T>(bool enableSwap)
            where T : unmanaged
        {
            return (s, stream, arr) =>
            {
                {
                    stream.WriteS32(arr.Length);
                    stream.WriteSpan<T>(arr, arr.Length, enableSwap);
                }
            };
        }

        private static Func<NetSerializer, Stream, T[]> MakeNativeArrayDecoder<T>(bool enableSwap)
            where T : unmanaged
        {
            return (s, stream) => stream.ReadArray<T>(stream.ReadS32(), enableSwap);
        }

        private static Action<NetSerializer, Stream, T[]> MakeArrayEncoder<T>(Action<NetSerializer, Stream, T> encoder,
            bool nc)
        {
            return (s, stream, arr) =>
            {
                stream.WriteS32(arr.Length);
                foreach (var e in arr) s.Serialize(e, nc, encoder);
            };
        }

        private static Func<NetSerializer, Stream, T[]> MakeArrayDecoder<T>(Func<NetSerializer, Stream, T> decoder,
            bool nc)
        {
            return (s, stream) =>
            {
                int count = stream.ReadS32();
                var res = new T[count];
                for (int i = 0; i < count; i++) res[i] = s.Deserialize(nc, decoder);
                return res;
            };
        }

        private static Action<NetSerializer, Stream, List<T>> MakeListEncoder<T>(
            Action<NetSerializer, Stream, T> encoder, bool nc)
        {
            return (s, stream, list) =>
            {
                stream.WriteS32(list.Count);
                foreach (var e in list) s.Serialize(e, nc, encoder);
            };
        }

        private static Func<NetSerializer, Stream, List<T>> MakeListDecoder<T>(Func<NetSerializer, Stream, T> decoder,
            bool nc)
        {
            return (s, stream) =>
            {
                int count = stream.ReadS32();
                var res = new List<T>(count);
                for (int i = 0; i < count; i++) res.Add(s.Deserialize(nc, decoder));
                return res;
            };
        }

        private static Action<NetSerializer, Stream, Dictionary<TKey, TValue>> MakeDictionaryEncoder<TKey, TValue>(
            Action<NetSerializer, Stream, TKey> keyEncoder, bool keyNc,
            Action<NetSerializer, Stream, TValue> valueEncoder,
            bool valueNc)
        {
            return (s, stream, dict) =>
            {
                stream.WriteS32(dict.Count);
                foreach (var kvp in dict)
                {
                    s.Serialize(kvp.Key, keyNc, keyEncoder);
                    s.Serialize(kvp.Value, valueNc, valueEncoder);
                }
            };
        }

        private static Func<NetSerializer, Stream, Dictionary<TKey, TValue>> MakeDictionaryDecoder<TKey, TValue>(
            Func<NetSerializer, Stream, TKey> keyDecoder, bool keyNc, Func<NetSerializer, Stream, TValue> valueDecoder,
            bool valueNc)
        {
            return (s, stream) =>
            {
                var dict = new Dictionary<TKey, TValue>();
                int count = stream.ReadS32();
                for (int i = 0; i < count; i++)
                {
                    var k = s.Deserialize(keyNc, keyDecoder);
                    var v = s.Deserialize(valueNc, valueDecoder);
                    if (k != null) dict[k] = v;
                }

                return dict;
            };
        }

        private static void StrEncoder(NetSerializer s, Stream stream, string str)
        {
            stream.WriteUtf8String(str);
        }

        private static string StrDecoder(NetSerializer s, Stream stream)
        {
            return stream.ReadUtf8String();
        }

        /// <summary>
        ///     Register a type's encoder and decoder
        /// </summary>
        /// <param name="encoder">Encoder for object to stream</param>
        /// <param name="decoder">Decoder for stream to object</param>
        /// <param name="generateCollectionConverters">Generate converters for basic collections</param>
        public static void Register<T>(Action<NetSerializer, Stream, T> encoder, Func<NetSerializer, Stream, T> decoder,
            bool generateCollectionConverters = true)
        {
            RegisterInternal(encoder, decoder);
            if (!generateCollectionConverters) return;
            bool nc = !typeof(T).IsValueType;
            RegisterInternal(MakeArrayEncoder(encoder, nc), MakeArrayDecoder(decoder, nc));
            RegisterInternal(MakeListEncoder(encoder, nc), MakeListDecoder(decoder, nc));
            RegisterInternal(MakeDictionaryEncoder<string, T>(StrEncoder, true, encoder, nc),
                MakeDictionaryDecoder(StrDecoder, true, decoder, nc));
        }

        /// <summary>
        ///     Register a type's encoder and decoder
        /// </summary>
        /// <param name="converters">Custom converter mapping</param>
        /// <param name="encoder">Encoder for object to stream</param>
        /// <param name="decoder">Decoder for stream to object</param>
        /// <param name="generateCollectionConverters">Generate converters for basic collections</param>
        public static void RegisterCustom<T>(Dictionary<Type, (object encoder, object decoder)> converters,
            Action<NetSerializer, Stream, T> encoder, Func<NetSerializer, Stream, T> decoder,
            bool generateCollectionConverters = true)
        {
            converters[typeof(T)] = (encoder, decoder);
            if (!generateCollectionConverters) return;
            bool nc = !typeof(T).IsValueType;
            converters[typeof(T[])] = (MakeArrayEncoder(encoder, nc), MakeArrayDecoder(decoder, nc));
            converters[typeof(List<T>)] = (MakeListEncoder(encoder, nc), MakeListDecoder(decoder, nc));
            converters[typeof(Dictionary<string, T>)] = (
                MakeDictionaryEncoder<string, T>(StrEncoder, true, encoder, nc),
                MakeDictionaryDecoder(StrDecoder, true, decoder, nc));
        }

        /// <summary>
        ///     Register dictionary type
        /// </summary>
        /// <typeparam name="TKey">Dictionary key type</typeparam>
        /// <typeparam name="TValue">Dictionary value type</typeparam>
        /// <exception cref="ApplicationException">If unregistered types are used</exception>
        public static void AddDictionary<TKey, TValue>()
        {
            if (!_converters.TryGetValue(typeof(TKey), out var resKey))
                throw new ApplicationException(
                    "Cannot add dictionary converters if dictionary key type is not already registered");
            if (!_converters.TryGetValue(typeof(TValue), out var resValue))
                throw new ApplicationException(
                    "Cannot add dictionary converters if dictionary value type is not already registered");
            (Action<NetSerializer, Stream, TKey> keyE, Func<NetSerializer, Stream, TKey> keyD) = (
                (Action<NetSerializer, Stream, TKey>)resKey.encoder,
                (Func<NetSerializer, Stream, TKey>)resKey.decoder);
            (Action<NetSerializer, Stream, TValue> valueE, Func<NetSerializer, Stream, TValue> valueD) = (
                (Action<NetSerializer, Stream, TValue>)resValue.encoder,
                (Func<NetSerializer, Stream, TValue>)resValue.decoder);
            bool ncKey = !typeof(TKey).IsValueType;
            bool ncValue = !typeof(TValue).IsValueType;
            RegisterInternal(MakeDictionaryEncoder(keyE, ncKey, valueE, ncValue),
                MakeDictionaryDecoder(keyD, ncKey, valueD, ncValue));
        }

        /// <summary>
        ///     Register dictionary type
        /// </summary>
        /// <param name="converters">Custom converter mapping</param>
        /// <typeparam name="TKey">Dictionary key type</typeparam>
        /// <typeparam name="TValue">Dictionary value type</typeparam>
        /// <exception cref="ApplicationException">If unregistered types are used</exception>
        public static void AddDictionaryCustom<TKey, TValue>(
            Dictionary<Type, (object encoder, object decoder)> converters)
        {
            if (!GetConverterCustom<TKey>(converters, out var key, true))
                throw new ApplicationException(
                    "Cannot add dictionary converters if dictionary key type is not already registered");
            if (!GetConverterCustom<TValue>(converters, out var value, true))
                throw new ApplicationException(
                    "Cannot add dictionary converters if dictionary value type is not already registered");
            (Action<NetSerializer, Stream, TKey> keyE, Func<NetSerializer, Stream, TKey> keyD) = key;
            (Action<NetSerializer, Stream, TValue> valueE, Func<NetSerializer, Stream, TValue> valueD) = value;
            bool ncKey = !typeof(TKey).IsValueType;
            bool ncValue = !typeof(TValue).IsValueType;
            converters[typeof(Dictionary<TKey, TValue>)] = (MakeDictionaryEncoder(keyE, ncKey, valueE, ncValue),
                MakeDictionaryDecoder(keyD, ncKey, valueD, ncValue));
        }

        /// <summary>
        ///     Get encoder/decoder pair for type
        /// </summary>
        /// <param name="res">Encoder and decoder</param>
        /// <typeparam name="T">Type to retrieve encoder/decoder for</typeparam>
        /// <returns>True if encoder/decoder were obtained</returns>
        public static bool GetConverter<T>(
            out (Action<NetSerializer, Stream, T> encoder, Func<NetSerializer, Stream, T> decoder) res)
        {
            if (_converters.TryGetValue(typeof(T), out var res1))
            {
                res = ((Action<NetSerializer, Stream, T>)res1.encoder, (Func<NetSerializer, Stream, T>)res1.decoder);
                return true;
            }

            res = default;
            return false;
        }

        /// <summary>
        ///     Get encoder/decoder pair for type
        /// </summary>
        /// <param name="converters">Custom converter mapping</param>
        /// <param name="res">Encoder and decoder</param>
        /// <param name="withGlobal">Enable global converters</param>
        /// <typeparam name="T">Type to retrieve encoder/decoder for</typeparam>
        /// <returns>True if encoder/decoder were obtained</returns>
        public static bool GetConverterCustom<T>(IReadOnlyDictionary<Type, (object encoder, object decoder)> converters,
            out (Action<NetSerializer, Stream, T> encoder, Func<NetSerializer, Stream, T> decoder) res,
            bool withGlobal = false)
        {
            res = default;
            if (!converters.TryGetValue(typeof(T), out var resX))
                return withGlobal && GetConverter(out res);
            if (!(resX.encoder is Action<NetSerializer, Stream, T> encoder)) return false;
            if (!(resX.decoder is Func<NetSerializer, Stream, T> decoder)) return false;
            res = (encoder, decoder);
            return true;
        }

        /// <summary>
        ///     Get encoder/decoder pair for type
        /// </summary>
        /// <param name="res">Encoder and decoder</param>
        /// <typeparam name="T">Type to retrieve encoder/decoder for</typeparam>
        /// <returns>True if encoder/decoder were obtained</returns>
        public bool GetConverterLocal<T>(
            out (Action<NetSerializer, Stream, T> encoder, Func<NetSerializer, Stream, T> decoder) res)
        {
            return CustomConverters != null && GetConverterCustom(CustomConverters, out res) || GetConverter(out res);
        }

        /// <summary>
        ///     Serialize an object
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <typeparam name="T">Type of object to be serialized</typeparam>
        /// <exception cref="ApplicationException">If attempting to serialize unregistered type</exception>
        public void Serialize<T>(T obj)
        {
            if (!GetConverterLocal<T>(out var res))
                throw new ApplicationException("Tried to serialize unregistered type");
            Serialize(obj, !typeof(T).IsValueType, res.encoder);
        }

        /// <summary>
        ///     Serialize an object
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <param name="nullCheck">Serialize null state</param>
        /// <typeparam name="T">Type of object to be serialized</typeparam>
        /// <exception cref="ApplicationException">If attempting to serialize unregistered type</exception>
        public void Serialize<T>(T obj, bool nullCheck)
        {
            if (!GetConverterLocal<T>(out var res))
                throw new ApplicationException("Tried to serialize unregistered type");
            Serialize(obj, nullCheck, res.encoder);
        }

        /// <summary>
        ///     Serialize an object
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <param name="nullCheck">Serialize null state</param>
        /// <param name="encoder">Encoder to use</param>
        /// <typeparam name="T">Type of object to be serialized</typeparam>
        /// <exception cref="ApplicationException">If attempting to serialize unregistered type</exception>
        public void Serialize<T>(T obj, bool nullCheck, Action<NetSerializer, Stream, T> encoder)
        {
            if (nullCheck)
            {
                BaseStream.WriteU8((byte)(obj != null ? 1 : 0));
                if (obj == null) return;
            }

            encoder.Invoke(this, BaseStream, obj);
        }

        /// <summary>
        ///     Deserialize an object
        /// </summary>
        /// <typeparam name="T">Type of object to be deserialized</typeparam>
        /// <exception cref="ApplicationException">If attempting to serialize unregistered type</exception>
        /// <returns>Deserialized object or null</returns>
        public T Deserialize<T>()
        {
            if (!GetConverterLocal<T>(out var res))
                throw new ApplicationException("Tried to deserialize unregistered type");
            return Deserialize(!typeof(T).IsValueType, res.decoder);
        }

        /// <summary>
        ///     Deserialize an object
        /// </summary>
        /// <param name="nullCheck">Deserialize null state</param>
        /// <typeparam name="T">Type of object to be deserialized</typeparam>
        /// <returns>Deserialized object or null</returns>
        public T Deserialize<T>(bool nullCheck)
        {
            if (!GetConverterLocal<T>(out var res))
                throw new ApplicationException("Tried to deserialize unregistered type");
            return Deserialize(nullCheck, res.decoder);
        }

        /// <summary>
        ///     Deserialize an object
        /// </summary>
        /// <param name="nullCheck">Deserialize null state</param>
        /// <param name="decoder">Decoder to use</param>
        /// <typeparam name="T">Type of object to be deserialized</typeparam>
        /// <returns>Deserialized object or null</returns>
        public T Deserialize<T>(bool nullCheck, Func<NetSerializer, Stream, T> decoder)
        {
            if (nullCheck && BaseStream.ReadU8() == 0) return default;
            return decoder.Invoke(this, BaseStream);
        }
    }
}

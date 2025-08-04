using System;
using System.Buffers.Binary;
using System.Linq;
using System.Runtime.InteropServices;

namespace Konsarpoo.Collections;

public class BuiltinSeriHelper
{
    protected static readonly ILookup<Type, int> SizeOf = new[]
    {
        (typeof(bool), sizeof(bool)),
        (typeof(byte), sizeof(byte)),
        (typeof(sbyte), sizeof(sbyte)),
        (typeof(char), sizeof(char)),
        (typeof(short), sizeof(short)),
        (typeof(ushort), sizeof(ushort)),
        (typeof(int), sizeof(int)),
        (typeof(uint), sizeof(uint)),
        (typeof(long), sizeof(long)),
        (typeof(ulong), sizeof(ulong)),
        (typeof(float), sizeof(float)),
        (typeof(double), sizeof(double)),
        (typeof(decimal), sizeof(decimal)),
        (typeof(DateTime), sizeof(long)), // DateTime is stored as Int64
        (typeof(DateTimeOffset), sizeof(long) + sizeof(long)), // DateTime + Offset
        (typeof(Guid), 16)
    }.ToLookup(x => x.Item1, x => x.Item2);
    
    public static ILookup<Type, (Func<object, byte[]> write, Func<byte[], object> read)> GetSeriLookup()
    {
        return new[]
        {
            (typeof(double), GetBytesFunc<double>(), ReadBytesFunc<double>()),
            (typeof(double?), GetBytesNullableFunc<double>(), ReadBytesFuncNullable<double>()),
            (typeof(float), GetBytesFunc<float>(), ReadBytesFunc<float>()),
            (typeof(float?), GetBytesNullableFunc<float>(), ReadBytesFuncNullable<float>()),
            (typeof(int), GetBytesFunc<int>(), ReadBytesFunc<int>()),
            (typeof(int?), GetBytesNullableFunc<int>(), ReadBytesFuncNullable<int>()),
            (typeof(uint), GetBytesFunc<uint>(), ReadBytesFunc<uint>()),
            (typeof(uint?), GetBytesNullableFunc<uint>(), ReadBytesFuncNullable<uint>()),
            (typeof(long), GetBytesFunc<long>(), ReadBytesFunc<long>()),
            (typeof(long?), GetBytesNullableFunc<long>(), ReadBytesFuncNullable<long>()),
            (typeof(ulong), GetBytesFunc<ulong>(), ReadBytesFunc<ulong>()),
            (typeof(ulong?), GetBytesNullableFunc<ulong>(), ReadBytesFuncNullable<ulong>()),
            (typeof(short), GetBytesFunc<short>(), ReadBytesFunc<short>()),
            (typeof(short?), GetBytesNullableFunc<short>(), ReadBytesFuncNullable<short>()),
            (typeof(ushort), GetBytesFunc<ushort>(), ReadBytesFunc<ushort>()),
            (typeof(ushort?), GetBytesNullableFunc<ushort>(), ReadBytesFuncNullable<ushort>()),
            (typeof(byte), GetBytesFunc<byte>(), ReadBytesFunc<byte>()),
            (typeof(byte?), GetBytesNullableFunc<byte>(), ReadBytesFuncNullable<byte>()),
            (typeof(sbyte), GetBytesFunc<sbyte>(), ReadBytesFunc<sbyte>()),
            (typeof(sbyte?), GetBytesNullableFunc<sbyte>(), ReadBytesFuncNullable<sbyte>()),
            (typeof(DateTime), GetBytesFunc<DateTime>(), ReadBytesFunc<DateTime>()),
            (typeof(DateTime?), GetBytesNullableFunc<DateTime>(), ReadBytesFuncNullable<DateTime>()),
            (typeof(DateTimeOffset), GetBytesFunc<DateTimeOffset>(), ReadBytesFunc<DateTimeOffset>()),
            (typeof(DateTimeOffset?), GetBytesNullableFunc<DateTimeOffset>(), ReadBytesFuncNullable<DateTimeOffset>()),
            (typeof(Guid), GetBytesFunc<Guid>(), ReadBytesFunc<Guid>()),
            (typeof(Guid?), GetBytesNullableFunc<Guid>(), ReadBytesFuncNullable<Guid>()),
        }.ToLookup(x => x.Item1, x => (x.Item2, x.Item3));
    }
    
    private static Func<object, byte[]> GetBytesFunc<T>() where T : struct
    {
        Func<object, byte[]> f = (arr) =>
        {
            Span<T> span = ((T[])arr).AsSpan();
            var asBytes = MemoryMarshal.AsBytes<T>(span);
            return asBytes.ToArray();
        };

        return f;
    }
    
    private static Func<object, byte[]> GetBytesNullableFunc<T>() where T : struct
    {
        var sizeOf = SizeOf[typeof(T)].First();
        
        Func<object, byte[]> f = (arr) =>
        {
            T?[] nullableArr = (T?[])arr;
            int len = nullableArr.Length;
            int bitmapBytes = (len + 7) / 8;
            int valueBytes = len * sizeOf;
            byte[] result = new byte[4 + bitmapBytes + valueBytes];

            // Write length
            BinaryPrimitives.WriteInt32LittleEndian(result, len);

            // Write bitmap and values
            int valueOffset = 4 + bitmapBytes;
            int bitIndex = 0;
            for (int i = 0; i < len; i++)
            {
                if (nullableArr[i].HasValue)
                {
                    result[4 + (i / 8)] |= (byte)(1 << (i % 8));
                }

                if (nullableArr[i].HasValue)
                {
                    var t = nullableArr[i].Value;
                    MemoryMarshal.Write(result.AsSpan(valueOffset, sizeOf), ref t);
                    valueOffset += sizeOf;
                }
            }
            return result;
        };

        return f;
    }
    
    private static Func<byte[], object> ReadBytesFuncNullable<T>() where T : struct
    {
        var sizeOf = SizeOf[typeof(T)].First();
        
        Func<byte[], object> f = (fb) =>
        {
            ReadOnlySpan<byte> span = fb;
            int len = BinaryPrimitives.ReadInt32LittleEndian(span);
            int bitmapBytes = (len + 7) / 8;
            T?[] result = new T?[len];
            int valueOffset = 4 + bitmapBytes;
            int valueSize = sizeOf;
            int valueIndex = valueOffset;

            for (int i = 0; i < len; i++)
            {
                bool hasValue = (span[4 + (i / 8)] & (1 << (i % 8))) != 0;
                if (hasValue)
                {
                    result[i] = MemoryMarshal.Read<T>(span.Slice(valueIndex, valueSize));
                    valueIndex += valueSize;
                }
                else
                {
                    result[i] = null;
                }
            }
            return result;
        };

        return f;
    }
    
    private static Func<byte[], object> ReadBytesFunc<T>() where T : struct
    {
        Func<byte[], object> f = (fb) =>
        {
            Span<byte> bytes = fb.AsSpan();
            Span<T> vals = MemoryMarshal.Cast<byte, T>(bytes);
            return vals.ToArray();
        };

        return f;
    }
}
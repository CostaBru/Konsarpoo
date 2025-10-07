using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;

namespace Konsarpoo.Collections.Data.Serialization;

/// <summary>
/// Data stream serialization with optional compression and encryption.
/// </summary>
[DebuggerDisplay("CompressMode = {m_compressionLevel}, HasKey = {m_key != null}, ArrayCount = {ArrayCount}, MaxSizeOfArray = {MaxSizeOfArray}, DataCount = {DataCount}, Version = {Version}, CanFlush = {CanFlush}")]
public class AdvancedDataStreamSerialization : DataStreamSerialization
{
    private readonly byte[] m_key;
    private readonly CompressionLevel m_compressionLevel;
    private readonly int m_ivSize = 16;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedDataStreamSerialization"/> class.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="encryptKey"></param>
    /// <param name="compressionLevel"></param>
    /// <param name="maxSizeOfArray"></param>
    /// <param name="arrayCapacity"></param>
    public AdvancedDataStreamSerialization(FileStream stream, byte[] encryptKey, CompressionLevel compressionLevel, int maxSizeOfArray, int arrayCapacity = 0) 
        : base(stream,  maxSizeOfArray, WritePipelineBytes, ReadPipelineBytes, arrayCapacity)
    {
        m_key = encryptKey;
        m_compressionLevel = compressionLevel;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AdvancedDataStreamSerialization"/> class.
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="encryptKey"></param>
    /// <param name="compressionLevel"></param>
    public AdvancedDataStreamSerialization(FileStream stream,  byte[] encryptKey, CompressionLevel compressionLevel) : base(stream, WritePipelineBytes, ReadPipelineBytes)
    {
        m_key = encryptKey;
        m_compressionLevel = compressionLevel;
    }
    
    private static byte[] Compress(byte[] data, CompressionLevel compressionLevel)
    {
        using var output = new MemoryStream();
        using var stream = new DeflateStream(output, compressionLevel);
        stream.Write(data, 0, data.Length);
        stream.Flush();
        var compress = output.ToArray();
        return compress;
    }

    private static byte[] Decompress(byte[] data)
    {
        using var input = new MemoryStream(data);
        using var output = new MemoryStream();
        using var stream = new DeflateStream(input, CompressionMode.Decompress);
        stream.CopyTo(output);
        stream.Flush();
        return output.ToArray();
    }
    
    private static byte[] WritePipelineBytes(DataStreamSerialization container, byte[] data)
    {
        var dataFileSerialization = (AdvancedDataStreamSerialization)container;

        byte[] key = dataFileSerialization.m_key;

        var bytes = data;

        if (key != null)
        {
            // Serialize first using base formatter

            using var aes = Aes.Create();
            aes.Key = NormalizeKey(key, 32); // use 256-bit key
            aes.GenerateIV();

            using var ms = new MemoryStream();
            // Prepend IV
            ms.Write(aes.IV, 0, aes.IV.Length);
            using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true);
            cs.Write(bytes, 0, bytes.Length);
            cs.FlushFinalBlock();

            bytes = ms.ToArray();
        }

        var compressionLevel = dataFileSerialization.m_compressionLevel;
        
        if (compressionLevel != CompressionLevel.NoCompression)
        {
            return Compress(bytes, compressionLevel);
        }

        return bytes;
    }

    protected static byte[] ReadPipelineBytes(DataStreamSerialization container, byte[] data)
    {
        var dataFileSerialization = (DataFileSerialization)container;

        var bytes = data;
        
        if (dataFileSerialization.m_compressionLevel != CompressionLevel.NoCompression)
        {
            bytes = Decompress(data);
        }
        
        byte[] key = dataFileSerialization.m_key;
        int ivSize = Math.Min(dataFileSerialization.m_ivSize, bytes.Length); // AES block size in bytes

        if (key != null)
        {
            var iv = new byte[ivSize];
            Buffer.BlockCopy(bytes, 0, iv, 0, ivSize);

            using var aes = Aes.Create();
            aes.Key = NormalizeKey(key, 32);
            aes.IV = iv;

            using var cipherMs = new MemoryStream(bytes, ivSize, bytes.Length - ivSize, writable: false);
            using var cs = new CryptoStream(cipherMs, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var plainMs = new MemoryStream();
            cs.CopyTo(plainMs);

            return plainMs.ToArray();
        }

        return bytes;
    }

    private static byte[] NormalizeKey(byte[] key, int targetSize)
    {
        if (key.Length == targetSize)
        {
            return key;
        }

        // If key length is one of AES supported sizes, accept as-is
        if (key.Length == 16 || key.Length == 24 || key.Length == 32)
        {
            if (targetSize == key.Length)
            {
                return key;
            }
        }

        // Otherwise, derive a 256-bit key via SHA-256 hash
        using var sha = SHA256.Create();
        var hashed = sha.ComputeHash(key);
        if (targetSize == hashed.Length)
        {
            return hashed;
        }
        // Fallback trim/pad to requested size
        var normalized = new byte[targetSize];
        Buffer.BlockCopy(hashed, 0, normalized, 0, Math.Min(targetSize, hashed.Length));
        return normalized;
    }
}
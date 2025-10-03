using System;
using System.IO;
using System.Security.Cryptography;

namespace Konsarpoo.Collections;

internal class CryptoDataFileSerialization : DataFileSerialization
{
    private readonly byte[] m_key;
    private const int IvSize = 16; // AES block size in bytes

    public CryptoDataFileSerialization(string filePath, FileMode fileMode, int maxSizeOfArray, byte[] key, int arrayCapacity = 0)
        : base(filePath, fileMode, maxSizeOfArray, arrayCapacity)
    {
        m_key = key;
    }

    public CryptoDataFileSerialization(string filePath, FileMode fileMode, byte[] key)
        : base(filePath, fileMode)
    {
        m_key = key;
    }

    protected override byte[] GetBytes<T>(T[] array)
    {
        // Serialize first using base formatter
        var plain = base.GetBytes(array);

        using var aes = Aes.Create();
        aes.Key = NormalizeKey(m_key, 32); // use 256-bit key
        aes.GenerateIV();

        using var ms = new MemoryStream();
        // Prepend IV
        ms.Write(aes.IV, 0, aes.IV.Length);
        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
        {
            cs.Write(plain, 0, plain.Length);
            cs.FlushFinalBlock();
        }
        return ms.ToArray();
    }

    protected override T[] ReadBytes<T>(byte[] data)
    {
        if (data == null || data.Length < IvSize)
        {
            throw new InvalidDataException("Encrypted payload is invalid or too small.");
        }

        var iv = new byte[IvSize];
        Buffer.BlockCopy(data, 0, iv, 0, IvSize);

        using var aes = Aes.Create();
        aes.Key = NormalizeKey(m_key, 32);
        aes.IV = iv;

        using var cipherMs = new MemoryStream(data, IvSize, data.Length - IvSize, writable: false);
        using var cs = new CryptoStream(cipherMs, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var plainMs = new MemoryStream();
        cs.CopyTo(plainMs);

        return base.ReadBytes<T>(plainMs.ToArray());
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


using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Konsarpoo.Collections.Data.Serialization;

/// <summary>
/// Data serialization to file.
/// </summary>
[DebuggerDisplay("FilePath = {m_filePath}, ArrayCount = {ArrayCount}, MaxSizeOfArray = {MaxSizeOfArray}, DataCount = {DataCount}, Version = {Version}, CanFlush = {CanFlush}")]
public class DataFileSerialization : AdvancedDataStreamSerialization
{
    private readonly string m_filePath;
    
    /// <summary>
    /// Data serialization to file constructor.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="fileMode"></param>
    /// <param name="encryptKey"></param>
    /// <param name="compressionLevel"></param>
    /// <param name="maxSizeOfArray"></param>
    /// <param name="arrayCapacity"></param>
    public DataFileSerialization(string filePath, FileMode fileMode, byte[] encryptKey, CompressionLevel compressionLevel, int maxSizeOfArray, int arrayCapacity = 0) 
        : base(new FileStream(filePath, fileMode, FileAccess.ReadWrite, FileShare.None), encryptKey, compressionLevel, maxSizeOfArray,  arrayCapacity)
    {
        m_filePath = filePath;
    }

    /// <summary>
    /// Data serialization to file constructor.
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="fileMode"></param>
    /// <param name="encryptKey"></param>
    /// <param name="compressionLevel"></param>
    public DataFileSerialization(string filePath, FileMode fileMode, byte[] encryptKey, CompressionLevel compressionLevel)
        : base(new FileStream(filePath, fileMode, FileAccess.ReadWrite, FileShare.None), encryptKey, compressionLevel)
    {
        m_filePath = filePath;
    }
    
    /// <summary>
    /// File path.
    /// </summary>
    public string FilePath => m_filePath;
}
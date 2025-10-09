namespace Konsarpoo.Collections.Data.Serialization;

/// <summary>
/// Interface for serializing and deserializing data with metadata support.
/// </summary>
public interface IDataSerializationInfo
{
    /// <summary>
    /// Updates metadata information.
    /// </summary>
    /// <param name="metaData"></param>
    void UpdateMetadata((int maxSizeOfArray, int dataCount, int version) metaData);

    /// <summary>
    /// Writes metadata information.
    /// </summary>
    void WriteMetadata();
    
    /// <summary>
    /// Reads metadata information.
    /// </summary>
    /// <returns></returns>
    void ReadMetadata();
    
    /// <summary>
    /// Appends an array of type T to the serialization stream.
    /// </summary>
    /// <param name="array"></param>
    /// <typeparam name="T"></typeparam>
    void AppendArray<T>(T[] array);

    /// <summary>
    /// Writes a single array of type T to the serialization stream.
    /// </summary>
    /// <param name="st"></param>
    /// <typeparam name="T"></typeparam>
    void WriteSingleArray<T>(T[] st);

    /// <summary>
    /// Reads an array of type T from the serialization stream at the specified index.
    /// </summary>
    /// <param name="arrayIndex"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T[] ReadArray<T>(int arrayIndex);

    /// <summary>
    /// Reads a single array of type T from the serialization stream.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T[] ReadSingleArray<T>();
    
    /// <summary>
    /// Gets total arrays stored.
    /// </summary>
    int ArrayCount { get; }
    
    /// <summary>
    /// Gets metadata.
    /// </summary>
    (int maxSizeOfArray, int dataCount, int version) MetaData { get; }

    /// <summary>
    /// Gets an extra metadata saved to the serialization stream.
    /// </summary>
    byte[] ExtraMetadata { get; }

    /// <summary>
    /// Sets extra metadata payload.
    /// </summary>
    /// <param name="metaDataBytes"></param>
    void SetExtraMetadata(byte[] metaDataBytes);
}


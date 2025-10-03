namespace Konsarpoo.Collections.Data.Serialization;

/// <summary>
/// Interface for serializing and deserializing data with metadata support.
/// </summary>
public interface IDataSerializationInfo
{
    /// <summary>
    /// Writes metadata information.
    /// </summary>
    /// <param name="metaData"></param>
    void WriteMetadata((int maxSizeOfArray, int dataCount, int version, int arraysCount) metaData);

    /// <summary>
    /// Reads metadata information.
    /// </summary>
    /// <returns></returns>
    (int maxSizeOfArray, int dataCount, int version, int arraysCount) ReadMetadata();
    
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
}


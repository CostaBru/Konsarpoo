namespace Konsarpoo.Collections;

public interface IDataSerializationInfo
{
    void WriteMetadata((int maxSizeOfArray, int dataCount, int version, int arraysCapacity) metaData);

    (int maxSizeOfArray, int dataCount, int version, int arraysCapacity) ReadMetadata();
    
    void AppendArray<T>(T[] array);

    void WriteSingleArray<T>(T[] st);

    T[] ReadArray<T>(int arrayIndex);

    T[] ReadSingleArray<T>();
}
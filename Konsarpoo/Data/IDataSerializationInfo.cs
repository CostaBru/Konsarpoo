namespace Konsarpoo.Collections;

public interface IDataSerializationInfo
{
    void WriteMetadata((int maxSizeOfArray, int dataCount, int version, int arraysCount) metaData);

    (int maxSizeOfArray, int dataCount, int version, int arraysCount) ReadMetadata();
    
    void AppendArray<T>(T[] array);

    void WriteSingleArray<T>(T[] st);

    T[] ReadArray<T>(int arrayIndex);

    T[] ReadSingleArray<T>();
}


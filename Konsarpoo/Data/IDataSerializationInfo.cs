namespace Konsarpoo.Collections;

internal interface IDataSerializationInfo
{
    void SetMetadata((int maxSizeOfArray, int dataCount, int version, int arraysCapacity) metaData);

    (int maxSizeOfArray, int dataCount, int version, int arraysCapacity) ReadMetaData();
    
    void AppendArray<T>(T[] array);

    void WriteSingleArray<T>(T[] st);

    T[] ReadArray<T>(int arrayIndex);

    T[] ReadSingleArray<T>();
}
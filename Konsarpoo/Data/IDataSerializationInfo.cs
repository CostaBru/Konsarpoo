namespace Konsarpoo.Collections;

public interface IDataSerializationInfo
{
    void WriteMetaData((int maxSizeOfArray, int dataCount, int version, int arraysCount) metaData);

    (int maxSizeOfArray, int dataCount, int version, int arraysCount) ReadMetaData();
    
    void AppendArray<T>(T[] array);

    void WriteSingleArray<T>(T[] st);

    T[] ReadArray<T>(int i);

    T[] ReadSingleArray<T>();
}
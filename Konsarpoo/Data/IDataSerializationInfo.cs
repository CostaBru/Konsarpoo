namespace Konsarpoo.Collections;

public interface IDataSerializationInfo
{
    void WriteMetaData((int maxSizeOfArray, int dataCount, int version, int arraysCount) metaData);

    (int maxSizeOfArray, int dataCount, int version, int arraysCount) ReadMetaData();
    
    void WriteArray<T>(int i, T[] array);

    void WriteSingleArray<T>(T[] st);

    T[] ReadArray<T>(int i);

    T[] ReadSingleArray<T>();
}
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Konsarpoo.Collections.Allocators;

public class GcAllocator<T> : IArrayAllocator<T>
{
    public static readonly int SmallHeapSuitableLength = GetSmallHeapSuitableLength<T>();

    private static MethodInfo GetSizeOfMethod<T>()
    {
        var assemblyName = new AssemblyName() { Name = "Konsarpoo.Internal" };

        var typeBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run).DefineDynamicModule(
            "Data").DefineType("Data", TypeAttributes.Public);

        var methodBuilder = typeBuilder.DefineMethod("SizeOf", MethodAttributes.Public | MethodAttributes.Static, typeof(int), null);
        var strArray = new string[1] { "T" };
        var parameterBuilderArray = methodBuilder.DefineGenericParameters(strArray);

        var ilGenerator = methodBuilder.GetILGenerator();

        ilGenerator.Emit(OpCodes.Sizeof, parameterBuilderArray[0]);
        ilGenerator.Emit(OpCodes.Ret);

        return typeBuilder.CreateTypeInfo().GetMethod("SizeOf").MakeGenericMethod(typeof(T));
    }

    /// <summary>
    /// Returns object size in bytes.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static int GetObjectSize<T>()
    {
        var type = typeof(T);

        if (type.IsValueType == false)
        {
            return IntPtr.Size;
        }

        return (int)GetSizeOfMethod<T>().Invoke(null, null); 
    }
       
    /// <summary>
    /// Returns max array size to prevent array placing to large object heap.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static int GetSmallHeapSuitableLength<T>()
    {
        return GetSmallHeapSuitableLength<T>(false);
    }
   
    private static int GetSmallHeapSuitableLength<T>(bool toPowerOf2)
    {
        int size = 80000 / GetObjectSize<T>();
        if (toPowerOf2)
        {
            int val = 1;
            while (val <= size)
            {
                val *= 2;
            }
            size = val / 2;
        }
        return size;
    }

    /// <inheritdoc />
    public T[] Rent(int count)
    {
        return new T[count];
    }

    /// <inheritdoc />
    public void Return(T[] array, bool clearArray = false)
    {
    }

    /// <inheritdoc />
    public bool CleanArrayReturn => true;
}
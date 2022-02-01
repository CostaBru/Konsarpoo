# Konsarpoo .NET  

The eco friendly set of array pool based collections for ``netstandard2.1``. Container's storage allocated and recycled back to shared memory pool by default. 

List of generic collections and APIs supported:

- ``Data``
- ``Map``
- ``Set``
- ``Stack``
- ``Queue``

Some extras built in:
- BitArr
- std::vector api
- Python like APIs. Append methods, ``+`` ``-``, equality operators overloaded.
- Lambda allocation free enumerable extensions

Each collection is serializable by default. It has a class destructor defined and ``System.IDisposable`` interface implemented to recycle internal storage on demand or by ``GC``. 

### DATA  

The universal random access data container. Supports ``List``, ``Array``, ``Stack`` and ``Queue`` API's. ``Data<T>`` has a minor overhead on adding items using default settings and huge advantage of reusing arrays to reduce ``GC`` collection time.
Implemented as a tree of ``.NET`` sub arrays. The array allocator and max size of array length per node may be set up for each instance, globally or globally for ``T``.

The ``DefaultMixedAllocator<T>`` instance is the default array allocator. It takes advantage of GC and pool array. For small arrays with length 65 or less, it uses ``GC`` for larger ones the ``System.Buffers.ArrayPool<T>.Shared`` class.

Here is how subtree random access support implemented:
```csharp
// Link node constructor:
this.m_stepBase = Math.Log(Math.Pow(1024, m_level - 1) * m_leafCapacity, 2);

// random access to contents:
 T ref this[int index] => get
 {
     int current = index << this.m_stepBase;
     int next = index - (current >> this.m_stepBase);
     return ref this.m_nodes.m_items[current][next];
 }
```

### MAP

``Map<K,V>`` is generic hashtable collection that supports the built in ``Dictionary`` API. To manage ``Map`` internal state the ``Konsarpoo.Collections.Data`` comes to the stage.  

The Map uses ``Data Array API`` to initialize or resize its storage to correct size:

```csharp
private void Initialize(int capacity)
{
    int prime = Prime.GetPrime(capacity);

    m_buckets = new Data<int>();
    m_entries = new Data<Entry>();
    m_entryValues = new Data<TValue>();

    m_buckets.Ensure(prime);
    m_entries.Ensure(prime);
    m_entryValues.Ensure(prime);

    m_freeList = -1;
 }
```

### SET

``Set<T>`` was designed to replace the ``System.Collections.Generic.HashSet`` container with ArrayPool based collection. Implemented in the same way as the ``Map<K,V>`` with keys collections only.

### STACK API / QUEUE API

The ``Konsarpoo.Collection.Data`` class supports ``Stack`` and ``Queue`` API with linear time performance for modifying methods. To access ``Queue`` API it is required to call ``Data.AsQueue()`` method which returns wrapper class with ``Data<T>`` as internal storage that keeps track of the start of queue.

### BITARR

It is a compact array of bit values, which are represented as Booleans. It uses ``Data<int>`` as internal storage.

# Performance

Here are the bunch of performance reports generated in Test\Benchmark\Reports folder. The source code of actual benchmarks, you can find in Test\Benchmark folder.

- ``Set_Array`` creates and fills array.
- ``List_Add`` creates and fills List.
- ``Data_Add`` creates and fills Data.
- ``Data_Ensure`` creates and fills Data as array.

``AMD Ryzen 7 4800H``, 16 logical and 8 physical cores ``.NET`` SDK=6.0.100

|      Method |       N |             Mean |          Error |          StdDev |           Median | Ratio | RatioSD |     Gen 0 |     Gen 1 |     Gen 2 |   Allocated |
|------------ |-------- |-----------------:|---------------:|----------------:|-----------------:|------:|--------:|----------:|----------:|----------:|------------:|
|   Set_Array |       2 |         5.828 ns |      0.0440 ns |       0.1290 ns |         5.843 ns |  0.30 |    0.01 |    0.0153 |         - |         - |        32 B |
|    List_Add |       2 |        19.568 ns |      0.1173 ns |       0.3328 ns |        19.481 ns |  1.00 |    0.00 |    0.0344 |         - |         - |        72 B |
|    Data_Add |       2 |        58.430 ns |      0.1560 ns |       0.4476 ns |        58.389 ns |  2.99 |    0.06 |    0.0382 |         - |         - |        80 B |
| Data_Ensure |       2 |        65.579 ns |      0.3118 ns |       0.9095 ns |        65.798 ns |  3.35 |    0.08 |    0.0381 |         - |         - |        80 B |
|             |         |                  |                |                 |                  |       |         |           |           |           |             |
|   Set_Array |    1000 |       851.116 ns |      1.4270 ns |       4.0713 ns |       850.757 ns |  0.30 |    0.01 |    1.9226 |         - |         - |     4,024 B |
| Data_Ensure |    1000 |       945.931 ns |      4.1302 ns |      11.3064 ns |       938.021 ns |  0.33 |    0.01 |    0.0572 |         - |         - |       120 B |
|    List_Add |    1000 |     2,883.931 ns |     32.0033 ns |      88.6809 ns |     2,833.490 ns |  1.00 |    0.00 |    4.0207 |         - |         - |     8,424 B |
|    Data_Add |    1000 |     3,063.148 ns |      6.1579 ns |      17.4689 ns |     3,068.149 ns |  1.06 |    0.03 |    0.3510 |         - |         - |       736 B |
|             |         |                  |                |                 |                  |       |         |           |           |           |             |
| Data_Ensure | 1000000 | 1,534,981.465 ns | 35,108.9647 ns |  97,286.7757 ns | 1,513,628.320 ns |  0.34 |    0.03 |         - |         - |         - |       120 B |
|   Set_Array | 1000000 | 1,640,578.975 ns |  6,920.6806 ns |  20,405.7830 ns | 1,646,434.082 ns |  0.36 |    0.01 |  998.0469 |  998.0469 |  998.0469 | 4,000,024 B |
|    Data_Add | 1000000 | 3,692,146.050 ns | 52,184.3540 ns | 153,047.6769 ns | 3,626,142.969 ns |  0.81 |    0.04 |         - |         - |         - |       736 B |
|    List_Add | 1000000 | 4,548,724.590 ns | 56,496.3994 ns | 165,694.1595 ns | 4,592,741.406 ns |  1.00 |    0.00 | 1992.1875 | 1992.1875 | 1992.1875 | 8,389,033 B |

``Map`` and ``Set`` has the similar performance for accessing data:

|                    Method | value |             Mean |          Error |         StdDev | Ratio | RatioSD |
|-------------------------- |------ |-----------------:|---------------:|---------------:|------:|--------:|
|     Dict_1000_ContainsKey |     0 |      7,903.82 ns |      37.446 ns |      35.027 ns |  1.00 |    0.00 |
|      Map_1000_ContainsKey |     0 |      7,928.91 ns |     155.854 ns |     145.786 ns |  1.00 |    0.02 |
|                           |       |                  |                |                |       |         |
|  Map_1000_000_ContainsKey |     0 | 26,984,303.57 ns |  74,167.674 ns |  65,747.711 ns |  0.99 |    0.00 |
| Dict_1000_000_ContainsKey |     0 | 27,322,500.45 ns |  84,866.853 ns |  75,232.255 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |       |         |
|        Dict_2_ContainsKey |     0 |         75.46 ns |       0.240 ns |       0.224 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     0 |         94.58 ns |       0.484 ns |       0.452 ns |  1.25 |    0.01 |
|                           |       |                  |                |                |       |         |
|     Dict_1000_ContainsKey |     1 |      7,933.07 ns |      54.466 ns |      50.948 ns |  1.00 |    0.00 |
|      Map_1000_ContainsKey |     1 |      7,999.10 ns |     158.456 ns |     148.220 ns |  1.01 |    0.02 |
|                           |       |                  |                |                |       |         |
| Dict_1000_000_ContainsKey |     1 | 25,900,549.22 ns |  37,198.088 ns |  29,041.820 ns |  1.00 |    0.00 |
|  Map_1000_000_ContainsKey |     1 | 26,975,849.11 ns | 150,622.633 ns | 133,523.041 ns |  1.04 |    0.01 |
|                           |       |                  |                |                |       |         |
|        Dict_2_ContainsKey |     1 |         74.04 ns |       1.020 ns |       0.904 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     1 |         95.88 ns |       0.459 ns |       0.430 ns |  1.29 |    0.01 |

# License

MIT
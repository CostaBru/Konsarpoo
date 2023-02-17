# Konsarpoo .NET  

[![Konsarpoo build, tests and coverage](https://github.com/CostaBru/Konsarpoo/actions/workflows/dotnet.yml/badge.svg)](https://github.com/CostaBru/Konsarpoo/actions/workflows/dotnet.yml) ![Coverage Badge](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/CostaBru/53438eb82c2cc9b70de34df4f14a7072/raw/Konsarpoo__head.json) ![GitHub Release Date](https://img.shields.io/github/release-date/CostaBru/Konsarpoo) ![Nuget](https://img.shields.io/nuget/dt/Konsarpoo)  ![GitHub search hit counter](https://img.shields.io/github/search/CostaBru/Konsarpoo/goto)

The eco friendly set of array pool based collections for ``netstandard2.1``. Container's storage allocated and recycled back to shared memory pool by default. 

List of generic collections and APIs supported:

- ``List``
- ``Map``
- ``Set``
- ``Stack``
- ``Queue``

Some extras built in:
- BitArr
- std::vector api
- Python like APIs. Append methods, ``+`` ``-``, equality operators overloaded.
- Lambda allocation free enumerable extensions
- Lfu Cache

Each collection (except ``Lfu Cache``) is serializable by default. It has a class destructor defined and ``System.IDisposable`` interface implemented to recycle internal storage on demand or by ``GC``. 

Possible use cases:
- Avoid allocating arrays in LOH
- Manage the available memory by providing custom allocator

## Nuget

Please use one of the following commands to install Konsarpoo.

#### Package Manager
```cmd
PM> Install-Package Konsarpoo -Version 2.0.4
```

#### .NET CLI
```cmd
> dotnet add package Konsarpoo --version 2.0.4
```

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
     m_buckets.Ensure(capacity);
     m_entries.Ensure(capacity);

     m_freeList = -1;
 }
```

### SET

``Set<T>`` was designed to replace the ``System.Collections.Generic.HashSet`` container with ArrayPool based collection. Implemented in the same way as the ``Map<K,V>`` with keys collections only.

### STACK API / QUEUE API

The ``Konsarpoo.Collections.Data`` class supports ``Stack`` and ``Queue`` API with linear time performance for modifying methods. To access ``Queue`` API it is required to call ``Data.AsQueue()`` method which returns wrapper class with ``Data<T>`` as internal storage that keeps track of the start of queue.

### BITARR

It is a compact array of bit values, which are represented as Booleans. It uses ``Data<int>`` as internal storage.

### LFU CACHE

A data structure which uses an O(1) algorithm of implementing LFU cache eviction scheme. 

https://github.com/papers-we-love/papers-we-love/blob/main/caching/a-constant-algorithm-for-implementing-the-lfu-cache-eviction-scheme.pdf

# Performance

Here are the bunch of performance reports generated in Test\Benchmark\Reports folder. The source code of actual benchmarks, you can find in Test\Benchmark folder.

- ``Set_Array`` creates and fills array.
- ``List_Add`` creates and fills List.
- ``Data_Add`` creates and fills Data.
- ``Data_Ensure`` creates and fills Data as array.

``AMD Ryzen 7 4800H``, 16 logical and 8 physical cores ``.NET`` SDK=6.0.100

|      Method |       N | NodeSize |            Mean |         Error |        StdDev |          Median | Ratio | RatioSD |     Gen 0 |     Gen 1 |     Gen 2 |   Allocated |
|------------ |-------- |----------|----------------:|--------------:|--------------:|----------------:|------:|--------:|----------:|----------:|----------:|------------:|
|   Set_Array |      10 | -        |        570.6 ns |      45.74 ns |      46.97 ns |        600.0 ns |  0.50 |    0.07 |         - |         - |         - |        64 B |
|    List_Add |      10 | -        |      1,162.5 ns |     177.82 ns |     174.64 ns |      1,100.0 ns |  1.00 |    0.00 |         - |         - |         - |       216 B |
|    Data_Add |      10 | 16       |      1,533.3 ns |      78.52 ns |      84.02 ns |      1,500.0 ns |  1.36 |    0.21 |         - |         - |         - |       328 B |
| Data_Ensure |      10 | 16       |      2,052.9 ns |     356.32 ns |     365.92 ns |      1,950.0 ns |  1.84 |    0.49 |         - |         - |         - |       240 B |
|             |         |          |                 |               |               |                 |       |         |           |           |           |             |
|   Set_Array |      10 | -        |        582.4 ns |      38.26 ns |      39.30 ns |        600.0 ns |  0.56 |    0.05 |         - |         - |         - |        64 B |
|    List_Add |      10 | -        |      1,037.5 ns |      90.12 ns |      88.51 ns |      1,000.0 ns |  1.00 |    0.00 |         - |         - |         - |       216 B |
|    Data_Add |      10 | 1048576  |      1,493.8 ns |      25.45 ns |      25.00 ns |      1,500.0 ns |  1.45 |    0.11 |         - |         - |         - |       328 B |
| Data_Ensure |      10 | 1048576  |      1,688.9 ns |      70.88 ns |      75.84 ns |      1,700.0 ns |  1.64 |    0.16 |         - |         - |         - |       240 B |
|             |         |          |                 |               |               |                 |       |         |           |           |           |             |
|   Set_Array |    1000 | -        |      1,066.7 ns |      96.17 ns |     102.90 ns |      1,000.0 ns |  0.32 |    0.03 |         - |         - |         - |     4,024 B |
|    List_Add |    1000 | -        |      3,294.4 ns |      81.55 ns |      87.26 ns |      3,300.0 ns |  1.00 |    0.00 |         - |         - |         - |     8,424 B |
|    Data_Add |    1000 | 16       |     44,900.0 ns |     672.61 ns |     719.68 ns |     45,000.0 ns | 13.64 |    0.30 |         - |         - |         - |     9,440 B |
| Data_Ensure |    1000 | 16       |     48,811.1 ns |     663.87 ns |     710.33 ns |     48,750.0 ns | 14.82 |    0.33 |         - |         - |         - |     5,656 B |
|             |         |          |                 |               |               |                 |       |         |           |           |           |             |
|   Set_Array |    1000 | -        |      1,278.9 ns |     439.30 ns |     488.28 ns |      1,100.0 ns |  0.40 |    0.15 |         - |         - |         - |     4,024 B |
|    List_Add |    1000 | -        |      3,241.2 ns |      49.40 ns |      50.73 ns |      3,200.0 ns |  1.00 |    0.00 |         - |         - |         - |     8,424 B |
| Data_Ensure |    1000 | 1048576  |      3,258.8 ns |      69.36 ns |      71.23 ns |      3,200.0 ns |  1.01 |    0.03 |         - |         - |         - |       112 B |
|    Data_Add |    1000 | 1048576  |      4,731.2 ns |      71.70 ns |      70.42 ns |      4,700.0 ns |  1.46 |    0.04 |         - |         - |         - |       760 B |
|             |         |          |                 |               |               |                 |       |         |           |           |           |             |
|   Set_Array | 1000000 | -        |  1,342,825.0 ns | 143,462.68 ns | 165,211.84 ns |  1,297,400.0 ns |  0.35 |    0.05 |         - |         - |         - | 4,000,024 B |
|    List_Add | 1000000 | -        |  3,850,315.0 ns | 175,356.63 ns | 201,940.96 ns |  3,805,500.0 ns |  1.00 |    0.00 | 1000.0000 | 1000.0000 | 1000.0000 | 8,389,080 B |
| Data_Ensure | 1000000 | 16       | 14,390,710.0 ns | 123,950.70 ns | 142,741.81 ns | 14,368,700.0 ns |  3.75 |    0.19 | 1000.0000 |         - |         - | 5,018,168 B |
|    Data_Add | 1000000 | 16       | 28,692,789.5 ns | 650,734.74 ns | 723,289.94 ns | 28,715,500.0 ns |  7.48 |    0.45 | 2000.0000 | 1000.0000 |         - | 8,768,016 B |
|             |         |          |                 |               |               |                 |       |         |           |           |           |             |
| Data_Ensure | 1000000 | 1048576  |  1,113,284.2 ns |  84,659.67 ns |  94,099.00 ns |  1,099,600.0 ns |  0.30 |    0.03 |         - |         - |         - |       112 B |
|   Set_Array | 1000000 | -        |  1,324,975.0 ns | 112,780.86 ns | 129,878.60 ns |  1,306,100.0 ns |  0.35 |    0.04 |         - |         - |         - | 4,000,024 B |
|    Data_Add | 1000000 | 1048576  |  2,631,444.4 ns |  28,874.18 ns |  30,895.04 ns |  2,626,700.0 ns |  0.70 |    0.03 |         - |         - |         - |       760 B |
|    List_Add | 1000000 | -        |  3,746,015.0 ns | 145,896.34 ns | 168,014.44 ns |  3,694,700.0 ns |  1.00 |    0.00 | 1000.0000 | 1000.0000 | 1000.0000 | 8,389,080 B |

``Map`` and ``Dictionary`` has the similar performance for accessing data :

|                    Method | value |             Mean |          Error |         StdDev |           Median | Ratio | RatioSD |
|-------------------------- |------ |-----------------:|---------------:|---------------:|-----------------:|------:|--------:|
|                           |       |                  |                |                |                  |       |         |
|     Dict_1000_ContainsKey |     7 |      7,569.34 ns |       6.146 ns |       6.312 ns |      7,568.64 ns |  1.00 |    0.00 |
|      Map_1000_ContainsKey |     7 |      7,922.20 ns |     108.648 ns |     111.573 ns |      8,014.10 ns |  1.05 |    0.02 |
|                           |       |                  |                |                |                  |       |         |
| Dict_1000_000_ContainsKey |     7 | 14,247,732.66 ns |  43,231.766 ns |  49,785.766 ns | 14,234,917.97 ns |  1.00 |    0.00 |
|  Map_1000_000_ContainsKey |     7 | 14,255,611.81 ns |  27,958.683 ns |  29,915.478 ns | 14,258,233.59 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |                  |       |         |
|        Dict_2_ContainsKey |     7 |         58.59 ns |       1.922 ns |       2.136 ns |         56.95 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     7 |         71.55 ns |       1.868 ns |       2.077 ns |         70.18 ns |  1.22 |    0.01 |
|                           |       |                  |                |                |                  |       |         |
|      Map_1000_ContainsKey |     8 |      7,584.76 ns |      14.297 ns |      15.891 ns |      7,578.59 ns |  0.99 |    0.01 |
|     Dict_1000_ContainsKey |     8 |      7,674.79 ns |     110.162 ns |     117.872 ns |      7,675.73 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |                  |       |         |
|  Map_1000_000_ContainsKey |     8 | 14,236,190.16 ns |  13,148.830 ns |  15,142.212 ns | 14,236,654.69 ns |  1.00 |    0.00 |
| Dict_1000_000_ContainsKey |     8 | 14,246,466.93 ns |  18,651.127 ns |  19,956.497 ns | 14,250,731.25 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |                  |       |         |
|        Dict_2_ContainsKey |     8 |         61.91 ns |       0.116 ns |       0.124 ns |         61.94 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     8 |         73.71 ns |       0.426 ns |       0.474 ns |         73.61 ns |  1.19 |    0.01 |
|                           |       |                  |                |                |                  |       |         |
|      Map_1000_ContainsKey |     9 |      7,575.19 ns |       3.236 ns |       3.463 ns |      7,574.51 ns |  0.98 |    0.02 |
|     Dict_1000_ContainsKey |     9 |      7,700.70 ns |     109.298 ns |     121.484 ns |      7,798.27 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |                  |       |         |
|  Map_1000_000_ContainsKey |     9 | 14,281,387.66 ns |  39,697.615 ns |  44,123.794 ns | 14,269,407.81 ns |  1.00 |    0.01 |
| Dict_1000_000_ContainsKey |     9 | 14,339,116.53 ns | 120,126.679 ns | 133,520.485 ns | 14,318,207.81 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |                  |       |         |
|        Dict_2_ContainsKey |     9 |         60.98 ns |       1.947 ns |       2.083 ns |         62.44 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     9 |         72.66 ns |       2.386 ns |       2.553 ns |         74.18 ns |  1.19 |    0.08 |


# License

MIT
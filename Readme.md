# Konsarpoo .NET  

[![Konsarpoo build, tests and coverage](https://github.com/CostaBru/Konsarpoo/actions/workflows/dotnet.yml/badge.svg)](https://github.com/CostaBru/Konsarpoo/actions/workflows/dotnet.yml) ![Coverage Badge](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/CostaBru/53438eb82c2cc9b70de34df4f14a7072/raw/Konsarpoo__head.json) ![GitHub Release Date](https://img.shields.io/github/release-date/CostaBru/Konsarpoo) ![Nuget](https://img.shields.io/nuget/dt/Konsarpoo)  ![GitHub search hit counter](https://img.shields.io/github/search/CostaBru/Konsarpoo/goto)

The eco friendly set of array pool based collections for ``netstandard2.1``. Container's storage allocated and recycled back to shared memory pool by default.  

List of generic collections and APIs supported:

- ``Array``
- ``List``
- ``Map``
- ``Set``
- ``Stack``
- ``Queue``

Some extras built in:
- ``BitArr``
- ``Lfu Cache``
- std::vector api
- Python like APIs. Append methods, ``+`` ``-``, equality operators overloaded.
- Lambda allocation free enumerable extensions

Each collection is serializable by default. It has a class destructor defined and ``System.IDisposable`` interface implemented to recycle internal storage on demand or by ``GC``. 

Possible use cases:
- Avoid allocating large arrays in LOH (please call - ``KonsarpooAllocatorGlobalSetup.SetGcAllocatorSetup()``)
- Manage the available memory by providing custom allocator (please call - ``KonsarpooAllocatorGlobalSetup.SetDefaultAllocatorSetup([your allocator impl])``)

## Nuget

Please use one of the following commands to install Konsarpoo.

#### Package Manager
```cmd
PM> Install-Package Konsarpoo -Version 2.1.0
```

#### .NET CLI
```cmd
> dotnet add package Konsarpoo --version 2.1.0
```

### DATA  

The universal random access data container. Supports ``List``, ``Array``, ``Stack`` and ``Queue`` API's. ``Data<T>`` has a minor overhead on adding items using default settings and huge advantage of reusing arrays to reduce ``GC`` collection time.
Implemented as a tree of ``.NET`` sub arrays. The array allocator and max size of array length per node may be set up for each instance, globally or globally for ``T``.

The ``GcArrayPoolMixedAllocator<T>`` instance is the default array allocator. It takes advantage of GC and pool array. For small arrays with length 65 or less, it uses ``GC`` for larger ones the ``System.Buffers.ArrayPool<T>.Shared`` class.

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

A data structure which uses an O(1) algorithm of implementing LFU cache eviction scheme. It has a map like API and simple cleanup methods to remove a certain number of non-relevant items from the cache. In addition to that it can keep track of cached data obsolescence by last accessed timestamp and remove those obsolete items on demand. 

https://github.com/papers-we-love/papers-we-love/blob/main/caching/a-constant-algorithm-for-implementing-the-lfu-cache-eviction-scheme.pdf

# Performance

Here are the bunch of performance reports generated in Test\Benchmark\Reports folder. The source code of actual benchmarks, you can find in Test\Benchmark folder.

- ``Set_Array`` creates and fills array.
- ``List_Add`` creates and fills List.
- ``Data_Add`` creates and fills Data.
- ``Data_Ensure`` creates and fills Data as array.

``AMD Ryzen 7 4800H``, 16 logical and 8 physical cores ``.NET`` SDK=6.0.100

|      Method |       N | NodeSize |            Mean |           Error |          StdDev |          Median | Ratio | RatioSD |     Gen 0 |     Gen 1 |     Gen 2 |   Allocated |
|------------ |-------- |--------- |----------------:|----------------:|----------------:|----------------:|------:|--------:|----------:|----------:|----------:|------------:|
|   Set_Array |      10 |       16 |        677.8 ns |        51.25 ns |        54.83 ns |        700.0 ns |  0.52 |    0.07 |         - |         - |         - |           - |
|    List_Add |      10 |       16 |      1,318.8 ns |       140.19 ns |       137.69 ns |      1,250.0 ns |  1.00 |    0.00 |         - |         - |         - |           - |
|    Data_Add |      10 |       16 |      2,650.0 ns |        66.09 ns |        70.71 ns |      2,600.0 ns |  2.03 |    0.20 |         - |         - |         - |           - |
| Data_Ensure |      10 |       16 |      3,782.4 ns |     1,000.34 ns |     1,027.28 ns |      3,400.0 ns |  2.94 |    1.01 |         - |         - |         - |           - |
|             |         |          |                 |                 |                 |                 |       |         |           |           |           |             |
|   Set_Array |      10 |  1048576 |        657.9 ns |        54.61 ns |        60.70 ns |        700.0 ns |  0.54 |    0.05 |         - |         - |         - |           - |
|    List_Add |      10 |  1048576 |      1,229.4 ns |        45.74 ns |        46.97 ns |      1,200.0 ns |  1.00 |    0.00 |         - |         - |         - |           - |
|    Data_Add |      10 |  1048576 |      3,416.7 ns |     1,345.69 ns |     1,439.87 ns |      2,750.0 ns |  2.81 |    1.17 |         - |         - |         - |           - |
| Data_Ensure |      10 |  1048576 |      3,423.5 ns |        42.58 ns |        43.72 ns |      3,400.0 ns |  2.79 |    0.11 |         - |         - |         - |           - |
|             |         |          |                 |                 |                 |                 |       |         |           |           |           |             |
|   Set_Array |    1000 |       16 |      5,294.4 ns |       503.26 ns |       538.49 ns |      5,100.0 ns |  0.85 |    0.07 |         - |         - |         - |     4,504 B |
|    List_Add |    1000 |       16 |      6,141.2 ns |       142.20 ns |       146.03 ns |      6,100.0 ns |  1.00 |    0.00 |         - |         - |         - |     8,904 B |
| Data_Ensure |    1000 |       16 |     29,183.3 ns |       151.21 ns |       161.79 ns |     29,200.0 ns |  4.76 |    0.11 |         - |         - |         - |     9,872 B |
|    Data_Add |    1000 |       16 |     32,293.8 ns |       886.45 ns |       870.61 ns |     31,950.0 ns |  5.25 |    0.16 |         - |         - |         - |     9,944 B |
|             |         |          |                 |                 |                 |                 |       |         |           |           |           |             |
|   Set_Array |    1000 |  1048576 |      5,075.0 ns |       101.82 ns |       100.00 ns |      5,100.0 ns |  0.80 |    0.05 |         - |         - |         - |     4,504 B |
|    List_Add |    1000 |  1048576 |      6,341.2 ns |       317.52 ns |       326.07 ns |      6,200.0 ns |  1.00 |    0.00 |         - |         - |         - |     8,904 B |
| Data_Ensure |    1000 |  1048576 |      8,900.0 ns |       153.29 ns |       150.55 ns |      8,800.0 ns |  1.40 |    0.07 |         - |         - |         - |           - |
|    Data_Add |    1000 |  1048576 |     11,582.4 ns |       125.25 ns |       128.62 ns |     11,500.0 ns |  1.83 |    0.09 |         - |         - |         - |           - |
|             |         |          |                 |                 |                 |                 |       |         |           |           |           |             |
|   Set_Array | 1000000 |       16 |  1,377,070.0 ns |   192,274.18 ns |   221,423.24 ns |  1,312,950.0 ns |  0.36 |    0.06 |         - |         - |         - | 4,000,504 B |
|    List_Add | 1000000 |       16 |  3,808,484.2 ns |   146,313.84 ns |   162,627.45 ns |  3,828,600.0 ns |  1.00 |    0.00 | 1000.0000 | 1000.0000 | 1000.0000 | 8,389,896 B |
| Data_Ensure | 1000000 |       16 | 20,519,350.0 ns | 3,102,191.74 ns | 3,572,488.62 ns | 18,023,300.0 ns |  5.45 |    1.06 | 1000.0000 |         - |         - | 8,070,128 B |
|    Data_Add | 1000000 |       16 | 27,617,344.4 ns | 1,192,095.84 ns | 1,275,529.19 ns | 27,022,500.0 ns |  7.27 |    0.43 | 1000.0000 |         - |         - | 8,070,168 B |
|             |         |          |                 |                 |                 |                 |       |         |           |           |           |             |
| Data_Ensure | 1000000 |  1048576 |    871,400.0 ns |    47,116.34 ns |    50,413.96 ns |    857,300.0 ns |  0.23 |    0.02 |         - |         - |         - |           - |
|   Set_Array | 1000000 |  1048576 |  1,221,236.8 ns |    89,554.07 ns |    99,539.11 ns |  1,195,700.0 ns |  0.32 |    0.03 |         - |         - |         - | 4,000,504 B |
|    Data_Add | 1000000 |  1048576 |  2,647,411.1 ns |   121,390.14 ns |   129,886.09 ns |  2,598,450.0 ns |  0.69 |    0.03 |         - |         - |         - |           - |
|    List_Add | 1000000 |  1048576 |  3,852,352.6 ns |   115,704.92 ns |   128,605.71 ns |  3,883,800.0 ns |  1.00 |    0.00 | 1000.0000 | 1000.0000 | 1000.0000 | 8,389,896 B |

``Map`` and ``Dictionary`` has the similar performance for accessing data :

|                    Method | value |             Mean |          Error |         StdDev | Ratio | RatioSD |
|-------------------------- |------ |-----------------:|---------------:|---------------:|------:|--------:|
|     Dict_1000_ContainsKey |     0 |      8,070.32 ns |      26.622 ns |      27.338 ns |  1.00 |    0.00 |
|      Map_1000_ContainsKey |     0 |      8,135.12 ns |      44.030 ns |      48.939 ns |  1.01 |    0.01 |
|                           |       |                  |                |                |       |         |
| Dict_1000_000_ContainsKey |     0 | 13,838,801.48 ns |  55,127.961 ns |  61,274.583 ns |  1.00 |    0.00 |
|  Map_1000_000_ContainsKey |     0 | 13,868,230.59 ns |  54,336.201 ns |  60,394.544 ns |  1.00 |    0.01 |
|                           |       |                  |                |                |       |         |
|        Dict_2_ContainsKey |     0 |         61.71 ns |       0.128 ns |       0.137 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     0 |         67.63 ns |       0.561 ns |       0.600 ns |  1.10 |    0.01 |
|                           |       |                  |                |                |       |         |
|      Map_1000_ContainsKey |     1 |      8,072.28 ns |      55.017 ns |      61.151 ns |  1.00 |    0.01 |
|     Dict_1000_ContainsKey |     1 |      8,089.80 ns |      59.382 ns |      66.003 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |       |         |
|  Map_1000_000_ContainsKey |     1 | 13,769,208.85 ns |  27,904.357 ns |  29,857.349 ns |  1.00 |    0.00 |
| Dict_1000_000_ContainsKey |     1 | 13,772,631.25 ns |  30,649.503 ns |  32,794.625 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |       |         |
|        Dict_2_ContainsKey |     1 |         61.71 ns |       0.201 ns |       0.215 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     1 |         69.50 ns |       0.604 ns |       0.672 ns |  1.13 |    0.01 |
|                           |       |                  |                |                |       |         |
|     Dict_1000_ContainsKey |     2 |      8,069.92 ns |      18.976 ns |      18.637 ns |  1.00 |    0.00 |
|      Map_1000_ContainsKey |     2 |      8,085.11 ns |      44.093 ns |      47.179 ns |  1.00 |    0.01 |
|                           |       |                  |                |                |       |         |
|  Map_1000_000_ContainsKey |     2 | 13,788,524.42 ns |  42,158.461 ns |  46,859.018 ns |  1.00 |    0.00 |
| Dict_1000_000_ContainsKey |     2 | 13,812,604.98 ns |  23,904.451 ns |  23,477.367 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |       |         |
|        Dict_2_ContainsKey |     2 |         63.34 ns |       0.481 ns |       0.554 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     2 |         67.63 ns |       0.306 ns |       0.327 ns |  1.07 |    0.01 |
|                           |       |                  |                |                |       |         |
|      Map_1000_ContainsKey |     3 |      8,049.57 ns |       7.367 ns |       7.882 ns |  0.99 |    0.01 |
|     Dict_1000_ContainsKey |     3 |      8,153.32 ns |      73.403 ns |      78.540 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |       |         |
|  Map_1000_000_ContainsKey |     3 | 13,801,832.58 ns |  54,202.732 ns |  62,419.947 ns |  0.99 |    0.01 |
| Dict_1000_000_ContainsKey |     3 | 13,889,393.92 ns | 113,386.902 ns | 121,322.715 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |       |         |
|        Dict_2_ContainsKey |     3 |         63.20 ns |       0.678 ns |       0.726 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     3 |         68.91 ns |       0.947 ns |       1.053 ns |  1.09 |    0.02 |
|                           |       |                  |                |                |       |         |
|     Dict_1000_ContainsKey |     4 |      8,139.69 ns |      82.968 ns |      95.546 ns |  1.00 |    0.00 |
|      Map_1000_ContainsKey |     4 |      8,143.41 ns |      94.512 ns |     108.840 ns |  1.00 |    0.02 |
|                           |       |                  |                |                |       |         |
| Dict_1000_000_ContainsKey |     4 | 13,747,482.52 ns |  17,886.368 ns |  17,566.805 ns |  1.00 |    0.00 |
|  Map_1000_000_ContainsKey |     4 | 13,861,111.64 ns |  86,040.641 ns |  95,633.944 ns |  1.01 |    0.01 |
|                           |       |                  |                |                |       |         |
|        Dict_2_ContainsKey |     4 |         62.44 ns |       0.488 ns |       0.562 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     4 |         70.66 ns |       2.118 ns |       2.354 ns |  1.13 |    0.04 |
|                           |       |                  |                |                |       |         |
|     Dict_1000_ContainsKey |     5 |      8,031.30 ns |     236.904 ns |     272.820 ns |  1.00 |    0.00 |
|      Map_1000_ContainsKey |     5 |      8,073.19 ns |      34.280 ns |      38.103 ns |  1.01 |    0.03 |
|                           |       |                  |                |                |       |         |
| Dict_1000_000_ContainsKey |     5 | 13,745,356.03 ns |   8,925.305 ns |   9,549.977 ns |  1.00 |    0.00 |
|  Map_1000_000_ContainsKey |     5 | 13,787,644.65 ns |  34,177.398 ns |  37,988.087 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |       |         |
|        Dict_2_ContainsKey |     5 |         62.67 ns |       0.266 ns |       0.307 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     5 |         69.50 ns |       1.280 ns |       1.423 ns |  1.11 |    0.02 |
|                           |       |                  |                |                |       |         |
|      Map_1000_ContainsKey |     6 |      8,055.09 ns |      16.505 ns |      17.660 ns |  0.99 |    0.00 |
|     Dict_1000_ContainsKey |     6 |      8,116.66 ns |      55.051 ns |      61.190 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |       |         |
|  Map_1000_000_ContainsKey |     6 | 13,765,171.59 ns |  20,752.604 ns |  23,066.465 ns |  0.99 |    0.00 |
| Dict_1000_000_ContainsKey |     6 | 13,845,794.45 ns |  43,792.731 ns |  50,431.774 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |       |         |
|        Dict_2_ContainsKey |     6 |         62.63 ns |       0.190 ns |       0.195 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     6 |         68.75 ns |       1.043 ns |       1.159 ns |  1.09 |    0.02 |
|                           |       |                  |                |                |       |         |
|     Dict_1000_ContainsKey |     7 |      7,969.63 ns |     100.447 ns |     107.477 ns |  1.00 |    0.00 |
|      Map_1000_ContainsKey |     7 |      8,041.45 ns |       8.048 ns |       8.265 ns |  1.01 |    0.01 |
|                           |       |                  |                |                |       |         |
|  Map_1000_000_ContainsKey |     7 | 13,787,516.32 ns |  55,973.538 ns |  62,214.439 ns |  1.00 |    0.00 |
| Dict_1000_000_ContainsKey |     7 | 13,819,723.67 ns |  50,763.521 ns |  58,459.346 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |       |         |
|        Dict_2_ContainsKey |     7 |         61.98 ns |       0.220 ns |       0.244 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     7 |         68.66 ns |       1.029 ns |       1.143 ns |  1.11 |    0.02 |
|                           |       |                  |                |                |       |         |
|     Dict_1000_ContainsKey |     8 |      7,984.22 ns |     136.703 ns |     140.384 ns |  1.00 |    0.00 |
|      Map_1000_ContainsKey |     8 |      8,043.55 ns |      10.366 ns |      11.091 ns |  1.01 |    0.02 |
|                           |       |                  |                |                |       |         |
|  Map_1000_000_ContainsKey |     8 | 13,764,054.78 ns |  21,464.100 ns |  22,042.051 ns |  0.99 |    0.01 |
| Dict_1000_000_ContainsKey |     8 | 13,926,526.09 ns |  76,378.002 ns |  87,957.021 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |       |         |
|        Dict_2_ContainsKey |     8 |         62.52 ns |       0.257 ns |       0.274 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     8 |         70.13 ns |       0.286 ns |       0.306 ns |  1.12 |    0.01 |
|                           |       |                  |                |                |       |         |
|      Map_1000_ContainsKey |     9 |      8,103.08 ns |      57.791 ns |      66.553 ns |  1.00 |    0.01 |
|     Dict_1000_ContainsKey |     9 |      8,110.07 ns |      57.604 ns |      66.337 ns |  1.00 |    0.00 |
|                           |       |                  |                |                |       |         |
| Dict_1000_000_ContainsKey |     9 | 13,748,020.80 ns |   8,303.602 ns |   8,155.247 ns |  1.00 |    0.00 |
|  Map_1000_000_ContainsKey |     9 | 13,875,106.33 ns |  60,451.533 ns |  69,616.075 ns |  1.01 |    0.00 |
|                           |       |                  |                |                |       |         |
|        Dict_2_ContainsKey |     9 |         62.70 ns |       0.323 ns |       0.372 ns |  1.00 |    0.00 |
|         Map_2_ContainsKey |     9 |         67.70 ns |       0.290 ns |       0.297 ns |  1.08 |    0.01 |

# License

MIT

﻿# Konsarpoo .NET  

[![Konsarpoo build, tests and coverage](https://github.com/CostaBru/Konsarpoo/actions/workflows/dotnet.yml/badge.svg)](https://github.com/CostaBru/Konsarpoo/actions/workflows/dotnet.yml) ![Coverage Badge](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/CostaBru/53438eb82c2cc9b70de34df4f14a7072/raw/Konsarpoo__head.json) ![GitHub Release Date](https://img.shields.io/github/release-date/CostaBru/Konsarpoo) ![Nuget](https://img.shields.io/nuget/dt/Konsarpoo)  

The eco friendly set of array pool based and ref struct collections for ``netstandard2.1``. 

Array pool container's storage allocated and recycled back to shared memory pool by default. It has up to 30% overhead on filling out with data and the similar performance for data look up.

Ref struct collections allow you to write high performance and zero allocation data processing code using up to 8mb of memory (1mb is default).

List of generic collections and APIs supported:

- ``Array``
- ``List``
- ``Map``
- ``Set``
- ``Stack``
- ``Queue``

Some extras built in:
- ``BitArr``
- ``String Trie Map``
- ``Tuple Trie Map``
- ``Lfu Cache``
- ``Lfu Cache String Trie``
- std::vector api
- Python like APIs. Append methods, ``+`` ``-``, equality operators overloaded.
- Lambda allocation free enumerable extensions

Each collection is serializable by default. All necessary generic collection interfaces are implemented. All collections have a copying constructor.

Data<> class interfaces:
- ``IList<>``
- ``IReadOnlyList<>``
- ``ICollection``
- ``IList``

Set<> class interfaces:
- ``ICollection<>,`` 
- ``IReadOnlyCollection<>``

Map<,> StringTrieMap<> TupleTrieMap<,,> LfuCache<,> classes interfaces:
- ``IDictionary<,>``
- ``ICollection<KeyValuePair<,>>``
- ``IEnumerable<KeyValuePair<,>>``
- ``IReadOnlyDictionary<,>``
- ``IReadOnlyCollection<KeyValuePair<,>>``

Each class in this package has destructor defined and ``System.IDisposable`` interface implemented to recycle internal storage on demand or by ``GC``. 

Possible use cases of this package:
- Server side code with minimum GC collection time.
- Avoiding allocating large arrays in LOH (please call - ``KonsarpooAllocatorGlobalSetup.SetGcAllocatorSetup()``)
- Managing the available memory by providing custom allocator (please call - ``KonsarpooAllocatorGlobalSetup.SetDefaultAllocatorSetup([your allocator impl])``)
- Boosting performance if data processing task can be split into a smaller ones and memory allocated per task can fit into the stack.

## Nuget

Please use one of the following commands to install Konsarpoo:

#### Package Manager
```cmd
PM> Install-Package Konsarpoo -Version 5.0.5
```

#### .NET CLI
```cmd
> dotnet add package Konsarpoo --version 5.0.5
```

### DATA  

The universal random access data container. Supports ``List``, ``Array``, ``Stack`` and ``Queue`` API's. ``Data<T>`` has a minor overhead on adding items using default settings and huge advantage of reusing arrays to reduce ``GC`` collection time.
Implemented as a tree of ``.NET`` sub arrays. The array allocator and max size of array length per node may be set up for each instance, globally or globally for ``T``.

The ``GcArrayPoolMixedAllocator<T>`` instance is the default array allocator. It takes advantage of GC and pool array. For small arrays with length 65 or less, it uses ``GC`` for larger ones the ``System.Buffers.ArrayPool<T>.Shared`` class.

### MAP

``Map<K,V>`` is generic hashtable collection that supports the built in ``Dictionary`` API. To manage ``Map`` internal state the ``Konsarpoo.Collections.Data`` comes to the stage.  

### SET

``Set<T>`` was designed to replace the ``System.Collections.Generic.HashSet`` container with ArrayPool based collection. Implemented in the same way as the ``Map<K,V>`` with keys collections only.

### STACK API / QUEUE API

The ``Data`` class supports ``Stack`` and ``Queue`` API with linear time performance for accessing and modifying methods. To access ``Queue`` API it is required to call ``Data.AsQueue()`` method which returns wrapper class with ``Data<T>`` as internal storage that keeps track of the start of queue.

### BITARR

It is a compact array of bit values, which are represented as Booleans. It uses ``Data<int>`` as internal storage.

### STRING TRIE MAP

``StringTrieMap<V>`` is generic map collection was designed to store string keys in more space efficient way. It supports the built in ``IDictionary`` API and implemented as trie data structure. It has ``O(k)`` value value access time, where ``k`` is the len of a key. 

``StringTrieMap<V>`` may contain 3 types of node: ``Link``, ``EndLink`` and ``Tail``. All node types have ``System.Char`` key.
- ``Link`` node may have one or many child nodes.
- ``EndLink`` node has a value and child nodes. 
- ``Tail`` has a value and it may contain key suffix as a ``Data<System.Char>``.

In addition to ``IDictionary`` API, there are methods for fetching values by prefix and suffix: ``WhereKeyStartsWith``, ``WhereKeyEndsWith``, ``WhereKeyContains``

### TUPLE TRIE MAP

``TupleTrieMap<T1..T5, TValue>`` is generic map collection was designed to store tuple keys in more space efficient way. It supports the built in ``IDictionary`` API and implemented as trie data structure. It has ``O(k)`` value value access time, where ``k`` is the length of a tuple.

``TupleTrieMap<T1..T5, TValue>`` may contain 3 types of node: ``Link``, ``EndLink`` and ``Tail``. All node types have ``System.object`` key taken from the tuple.
- ``Link`` node may have one or many child nodes.
- ``EndLink`` node has a value and child nodes.
- ``Tail`` has a value and it may contain key tuple suffix as a ``Data<System.object>``.

It is a good alternative to ``Map`` of tuples. It works in the same way as ``StringTrieMap<V>`` with the following difference. Each node contains a whole tuple key part. So for instance, if you have defined a ``TupleTrieMap`` to store the following key type ``(int, bool, string, TimeSpan, string)`` then the width of node's chain will be equal to 5 (+1). To find the value for this key  ``StringTrieMap<V>`` iterates tuple items in the given key and traverses nodes sequentially: root -> int ``O(1)`` -> bool ``O(1)`` -> string ``O(1)``-> TimeSpan ``O(1)``-> string ``O(1)``

In addition to ``IDictionary`` API, there is a method for fetching values by a part of the tuple: ``WhereKeyStartsWith``

### LFU CACHE

A data structure which uses an ``O(1)`` algorithm of implementing LFU cache eviction scheme. It has a map like API and simple cleanup methods to remove a certain number of non-relevant items from the cache. 

In addition to that it can keep track both:
-  of cached data obsolescence by last accessed timestamp and remove those obsolete items on demand.
-  of total memory used by cache and remove obsolete and least accessed keys to insert a new item.

User can define which type this cache should use as a collection of keys and set of frequencies.

https://github.com/papers-we-love/papers-we-love/blob/main/caching/a-constant-algorithm-for-implementing-the-lfu-cache-eviction-scheme.pdf

### LFU CACHE STRING TRIE

A LFU CACHE data structure where string keys stored in ``StringTrieMap<V>``.

# Stackalloc and ref struct 

There are basic collections implemented:

- ``DataRs``
- ``MapRs``
- ``SetRs``
- ``QueueRs``
- ``StackRs``

It turned out that overall performance of stackalloc containers are up to 10 times faster in compare to heap allocated one.

Examples of usages:

``DataRs``
```csharp 
Span<int> memory = stackalloc int[10_000];
var dataRs = new DataRs<int>(ref memory);

dataRs.Add(1);

var first = dataRs.FirstOrDefault();

Assert.AreEqual(1, first);
Assert.AreEqual(1, dataRs.Count);
```

# License

MIT

# Konsarpoo .NET  

The eco friendly set of array pool based collections for ``netstandard2.1``. Container's storage allocated and recycled back to shared memory pool by default. 

List of generic collections and APIs supported:

- Data
- Map
- Set
- Stack
- Queue

Some extras built in:
- BitArr
- std::vector api
- Python like APIs. Append methods, ``+`` ``-``, equality operators overloaded.
- Lambda allocation free enumerable extensions

Each collection is serializable by default. It has a class destructor defined and ``System.IDisposable`` interface implemented to recycle internal storage on demand or by ``GC``. 

# Quick start

### DATA  

The universal random access data container. Supports ``List``, ``Array``, ``Stack`` and ``Queue`` API's. ``Data<T>`` has a minor overhead on adding items using default settings and huge advantage of reusing arrays to reduce ``GC`` collection time.
Implemented as a tree of ``.NET`` sub arrays. The array allocator and max size of array length per node may be set up for each instance, globally or globally for ``T``.
The ``System.Buffers.ArrayPool<T>.Shared`` instance is the default allocator.

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

``Map<K,V>`` is generic hashtable collection that supports the built in ``Dictionary`` API. Uses ``Konsarpoo.Collections.Data`` to store hashes and values. 

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

``Set<T>`` is designed to replace the ``System.Collections.Generic.HashSet`` container with ArrayPool based collection. Implemented in the same way as the ``Map<K,V>`` with keys collections only.

### STACK API / QUEUE API

The ``Konsarpoo.Collection.Data`` class supports ``Stack`` and ``Queue`` API with linear time performance for modifying methods. To access ``Queue`` API it is required to call ``Data.AsQueue()`` method which returns wrapper class with ``Data<T>`` as internal storage that keeps track of the start of queue.

### BITARR

It is a compact array of bit values, which are represented as Booleans uses ``Data<int>`` as internal storage.

## Extensions
todo
# Performance
todo
# License

MIT
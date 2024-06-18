using System;
using System.Collections.Generic;

namespace Konsarpoo.Collections;

public class LfuCacheStringTrie<TValue> : LfuCache<string, TValue>
{
    public LfuCacheStringTrie() : this(true)
    {
    }

    public LfuCacheStringTrie(bool caseSensitive, 
        Func<TValue, TValue> copyStrategy = null, 
        Action<IEnumerable<char>, TValue> disposingStrategy = null) 
        : base(new StringTrieMap<DataVal>(caseSensitive), () => new StringTrieSet(caseSensitive), copyStrategy, disposingStrategy)
    {
    }

    private StringTrieMap<DataVal> Storage => (StringTrieMap<DataVal>)MapStorage;
    
    /// <summary>
    /// Returns values where key ends with a given substring. If substring is empty it returns all values.
    /// </summary>
    /// <param name="substring"></param>
    /// <returns></returns>
    public IEnumerable<TValue> WhereKeyEndsWith(string substring)
    {
        foreach (var val in Storage.WhereKeyEndsWith(substring))
        {
            yield return val.Value;
        }
    }
    
    /// <summary>
    /// Returns values where key starts with a given substring. If substring is empty it returns all values.
    /// </summary>
    /// <param name="substring"></param>
    /// <returns></returns>
    public IEnumerable<TValue> WhereKeyStartsWith(string substring)
    {
        foreach (var val in Storage.WhereKeyStartsWith(substring))
        {
            yield return val.Value;
        }
    }

    /// <summary>
    /// Returns values where key contains a given substring. If substring is empty it returns all values.
    /// </summary>
    /// <param name="substring"></param>
    /// <returns></returns>
    public IEnumerable<TValue> WhereKeyContains(string substring)
    {
        foreach (var val in Storage.WhereKeyContains(substring))
        {
            yield return val.Value;
        }
    }
}
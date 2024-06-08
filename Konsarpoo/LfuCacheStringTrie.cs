using System.Collections.Generic;

namespace Konsarpoo.Collections;

public class LfuCacheStringTrie<TValue> : LfuCache<IEnumerable<char>, TValue>
{
    public LfuCacheStringTrie() : this(true)
    {
    }

    public LfuCacheStringTrie(bool caseSensitive) : base(new StringTrieMap<DataVal>(caseSensitive), () => new StringTrieSet(caseSensitive))
    {
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary;

public class MutablePair<TKey, TValue>
{
    public TKey Key { get; set; }
    public TValue Value { get; set; }

    public MutablePair(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }

    public MutablePair()
    {
        Key = default!;
        Value = default!;
    }

    public TValue GetValue(TKey key)
    {
        if (!EqualityComparer<TKey>.Default.Equals(Key, key))
            throw new KeyNotFoundException($"Key '{key}' not found in the pair!");

        return Value;
    }
}

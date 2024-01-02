using System.Collections;

namespace paralel_4;

public class ConcurrentDictionary<TKey, TValue> : IEnumerable where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();
    private readonly object _lock = new object();

    public void AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
    {
        lock (_lock)
        {
            if (_dictionary.TryGetValue(key, out var existingValue))
            {
                _dictionary[key] = updateValueFactory(key, existingValue);
            }
            else
            {
                _dictionary[key] = addValue;
            }
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        lock (_lock)
        {
            return _dictionary.TryGetValue(key, out value);
        }
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        lock (_lock)
        {
            return new Dictionary<TKey, TValue>(_dictionary).GetEnumerator();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
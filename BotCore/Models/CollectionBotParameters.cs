using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace BotCore.Models
{
    public record class CollectionBotParameters : IDictionary<string, object>
    {
        private readonly Dictionary<string, object> _otherParameters = [];

        public ICollection<string> Keys => ((IDictionary<string, object>)_otherParameters).Keys;

        public ICollection<object> Values => ((IDictionary<string, object>)_otherParameters).Values;

        public int Count => ((ICollection<KeyValuePair<string, object>>)_otherParameters).Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, object>>)_otherParameters).IsReadOnly;

        public object this[string key]
        {
            get => _otherParameters[key];
            set => _otherParameters[key] = value;
        }

        public CollectionBotParameters(Dictionary<string, object>? otherParameters = null)
            => _otherParameters=otherParameters ?? [];
        public CollectionBotParameters(IEnumerable<KeyValuePair<string, object>> collection) : this(new Dictionary<string, object>(collection)) { }
        public CollectionBotParameters(CollectionBotParameters? collectionBot)
            => _otherParameters=collectionBot?._otherParameters ?? [];

        public bool ContainsKey(string key) => _otherParameters.ContainsKey(key);
        public bool TryGetParameter<T>(string key, out T? value)
        {
            if (_otherParameters.TryGetValue(key, out object? valueO) && valueO is T valueR)
            {
                value = valueR;
                return true;
            }
            value = default;
            return false;
        }

        public void Add(string key, object value)
        {
            ((IDictionary<string, object>)_otherParameters).Add(key, value);
        }

        public bool Remove(string key)
        {
            return ((IDictionary<string, object>)_otherParameters).Remove(key);
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out object value)
        {
            return ((IDictionary<string, object>)_otherParameters).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            ((ICollection<KeyValuePair<string, object>>)_otherParameters).Add(item);
        }

        public void Clear()
        {
            ((ICollection<KeyValuePair<string, object>>)_otherParameters).Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)_otherParameters).Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<string, object>>)_otherParameters).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return ((ICollection<KeyValuePair<string, object>>)_otherParameters).Remove(item);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return ((IEnumerable<KeyValuePair<string, object>>)_otherParameters).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_otherParameters).GetEnumerator();
        }


        public static implicit operator CollectionBotParameters(Dictionary<string, object> collection) => new(collection);
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace KezymaWeb.WholeHistoryRating.Collection
{
    public class DefaultDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        private TValue _defaultValue;
        public DefaultDictionary(TValue defaultValue)
        {
            _defaultValue = defaultValue;
        }
        public new TValue this[TKey key]
        {
            get
            {
                return TryGetValue(key, out TValue val) ? val : _defaultValue;
            }
            set
            {
                if (ContainsKey(key)) base[key] = value;
                else Add(key, value);
            }
        }
    }
}

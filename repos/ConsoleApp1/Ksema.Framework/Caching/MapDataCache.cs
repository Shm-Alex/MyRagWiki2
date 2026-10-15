using System;
using System.Collections.Generic;
using Framework.Utils;

namespace Ksema.Framework.Caching
{
    public class MapDataCache<TKey, TValue> : TableDataCacheBase<TKey,TValue>
    {
        private readonly C5.MultiHashDictionary<TKey, TValue> _map;

		public MapDataCache(string name, Func<IEnumerable<KeyValuePair<TKey, TValue>>> getAllValues, IEqualityComparer<TKey> keyComparer)
			:this(name, getAllValues, keyComparer, false)
		{
		}

    	public MapDataCache(string name, Func<IEnumerable<KeyValuePair<TKey, TValue>>> getAllValues, IEqualityComparer<TKey> keyComparer, bool overwrite) 
            : base(name, getAllValues, keyComparer, overwrite)
        {
            _map = new C5.MultiHashDictionary<TKey, TValue>(KeyComparer);
        }

		public MapDataCache(string name, Func<TKey, TValue> getValue, IEqualityComparer<TKey> keyComparer)
			: this(name, getValue, keyComparer, false)
		{
		}

		public MapDataCache(string name, Func<TKey, TValue> getValue, IEqualityComparer<TKey> keyComparer, bool overwrite) 
            : base(name, getValue, keyComparer, overwrite)
        {
            _map = new C5.MultiHashDictionary<TKey, TValue>(KeyComparer);
        }

		protected override TValue[] DoClearKey(TKey key)
		{
			TValue[] oldValues = _map[key].ToArray();
			_map.Remove(key);
			return oldValues;
		}

        protected override void DoClear()
        {
            _map.Clear();
        }

        protected override void DoAddItem(TKey key, TValue value)
        {
            _map.Add(key, value);
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> GetCurrentItems()
        {
			foreach (C5.KeyValuePair<TKey, TValue> entry in _map.AllEntries)
				yield return new KeyValuePair<TKey, TValue>(entry.Key, entry.Value);
        }

        public TValue[] this[TKey key]
        {
            get 
            {
                AssertUtils.IsNotNull(key, "key");
				RWLock.EnterUpgradeableReadLock();
				try
				{
					if (!_map.Contains(key))
						Initialize(key);

					var result = _map[key].ToArray();
					return result;
				}
				finally
				{
					RWLock.ExitUpgradeableReadLock();
				}
            }
        }

        public override bool ContainsKey(TKey key)
        {
			RWLock.EnterReadLock();
			try
			{
				return _map.Contains(key);
			}
			finally
			{
				RWLock.ExitReadLock();
			}

        }
    }
}
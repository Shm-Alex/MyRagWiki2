using System;
using System.Collections.Generic;
using Framework.Utils;

namespace Ksema.Framework.Caching
{
    public class DictionaryDataCache<TKey, TValue> : TableDataCacheBase<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _table;

		public DictionaryDataCache(string name, Func<IEnumerable<KeyValuePair<TKey, TValue>>> getAllValues, IEqualityComparer<TKey> keyComparer)
			: this(name, getAllValues, keyComparer, false)
		{
		}

        public DictionaryDataCache(string name, Func<IEnumerable<KeyValuePair<TKey, TValue>>> getAllValues, IEqualityComparer<TKey> keyComparer, bool overwrite) 
            : base(name, getAllValues, keyComparer, overwrite)
        {
            _table = new Dictionary<TKey,TValue>(KeyComparer);
        }

		public DictionaryDataCache(string name, Func<TKey, TValue> getValue, IEqualityComparer<TKey> keyComparer)
			: this(name, getValue, keyComparer, false)
		{
		}

        public DictionaryDataCache(string name, Func<TKey, TValue> getValue, IEqualityComparer<TKey> keyComparer, bool overwrite)
            : base(name, getValue, keyComparer, overwrite)
        {
            _table = new Dictionary<TKey,TValue>(KeyComparer);
        }

		public DictionaryDataCache(string name, Func<TKey, TValue> getValue)
			: this(name, getValue, null, false)
		{
		}

        public DictionaryDataCache(string name, Func<TKey, TValue> getValue, bool overwrite)
            : this(name, getValue, null, overwrite)
        {
        }

		public DictionaryDataCache(string name, Func<IEnumerable<KeyValuePair<TKey, TValue>>> getAllValues)
			: this(name, getAllValues, null, false)
		{
		}

        public DictionaryDataCache(string name, Func<IEnumerable<KeyValuePair<TKey, TValue>>> getAllValues, bool overwrite)
            : this(name, getAllValues, null, overwrite)
        {
        }

        public override bool ContainsKey(TKey key)
        {
			RWLock.EnterReadLock();
			try
			{
				return _table.ContainsKey(key);
			}
			finally
			{
				RWLock.ExitReadLock();
			}

        }

		protected override TValue[] DoClearKey(TKey key)
		{
			TValue value;
			if (_table.TryGetValue(key, out value))
			{
				_table.Remove(key);
				return new[] {value};
			}
			return new TValue[0];
		}

        protected override void DoClear()
        {
            _table.Clear();
        }

        protected override void DoAddItem(TKey key, TValue value)
        {
            _table[key] = value;
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> GetCurrentItems()
        {
            return _table;
        }

        /// <summary>
        /// Если элемента еще нет - добавляет переданный в параметре и возвращает его же
        /// Иначе возвращает существующий элемент
        /// </summary>
        public TValue EnsureValueCached(TKey key, TValue value)
        {
            AssertUtils.IsNotNull(key, "key");
			RWLock.EnterUpgradeableReadLock();
			try
			{
				TValue result;
				if (!_table.TryGetValue(key, out result))
				{
					RWLock.EnterWriteLock();
					try
					{
						result = value;
						AddItem(key, value);
					}
					finally
					{
						RWLock.ExitWriteLock();
					}
				}
				return result;
			}
			finally
			{
				RWLock.ExitUpgradeableReadLock();
			}
        }

        public bool TryGetValue(TKey key, out TValue result)
        {
            AssertUtils.IsNotNull(key, "key");
			RWLock.EnterUpgradeableReadLock();
			try
			{
				if (!_table.TryGetValue(key, out result))
				{
					Initialize(key);
					return _table.TryGetValue(key, out result);
				}

				return true;
			}
			finally
			{
				RWLock.ExitUpgradeableReadLock();
			}
        }

        public TValue this[TKey key]
        {
            get 
            {
                TValue result;
                if (!TryGetValue(key, out result))
                    throw new NotFoundException("Value not found for the key: " + key);
                return result;
            }
            set 
            {
                Add(key, value);
            }
        }

    }
}
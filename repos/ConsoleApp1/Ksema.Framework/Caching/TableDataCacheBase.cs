using System;
using System.Collections.Generic;
using Framework.Utils;

namespace Ksema.Framework.Caching
{
    public abstract class TableDataCacheBase<TKey,TValue> : DataCacheBase
    {
        private readonly Func<TKey, TValue> _getSingleValue;
        private readonly Func<IEnumerable<KeyValuePair<TKey, TValue>>> _getAllValues;

        private readonly IEqualityComparer<TKey> _keyComparer;
        private IEqualityComparer<TValue> _valueComparer;

        private C5.MultiHashDictionary<TValue, TKey> _reverseMap;
        private bool _allValuesSet;

    	protected TableDataCacheBase(
			string name, 
			Func<IEnumerable<KeyValuePair<TKey, TValue>>> getAllValues, 
			IEqualityComparer<TKey> keyComparer,
			bool overwrite) 
            : base(name, overwrite)
        {
            AssertUtils.IsNotNull(getAllValues, "getAllValues");
            _getAllValues = getAllValues;
            _keyComparer = keyComparer;
			AllowNullValues = true;
		}


    	protected TableDataCacheBase(
			string name, 
			Func<TKey, TValue> getValue, 
			IEqualityComparer<TKey> keyComparer,
			bool overwrite)
            : base(name, overwrite)
        {
            AssertUtils.IsNotNull(getValue, "getValue");
            _getSingleValue = getValue;
            _keyComparer = keyComparer;
			AllowNullValues = true;
		}

		public bool AllowNullValues
		{
			get;
			set;
		}

        public void EnableReverseMap()
        {
			AllowNullValues = false;
            EnableReverseMap(null);
        }

        public void EnableReverseMap(IEqualityComparer<TValue> valueCmp)
        {
            if (_getAllValues == null)
                throw new InvalidOperationException("getAllValues function not set");

			RWLock.EnterWriteLock();
			try
			{
				_valueComparer = valueCmp;
				_reverseMap = new C5.MultiHashDictionary<TValue, TKey>(ValueComparer, KeyComparer);
				GetCurrentItems().ForEach(item => _reverseMap.Add(item.Value, item.Key));
			}
			finally
			{
				RWLock.ExitWriteLock();
			}
        }

        public void Add(TKey key, TValue value)
        {
			RWLock.EnterWriteLock();
			try
			{
				AddItem(key, value);
			}
			finally
			{
				RWLock.ExitWriteLock();
			}
        }

		public void ClearKey(TKey key)
		{
			RWLock.EnterWriteLock();
			try
			{
				TValue[] oldValues = DoClearKey(key);
				if (_reverseMap != null)
				{
					_reverseMap.RemoveMany(oldValues);
				}
			}
			finally
			{
				RWLock.ExitWriteLock();
			}
		}

    	public override void Clear()
        {
			RWLock.EnterWriteLock();
			try
			{
				DoClear();
				if (_reverseMap != null)
					_reverseMap.Clear();
				_allValuesSet = false;
			}
			finally
			{
				RWLock.ExitWriteLock();
			}
        }

        public TKey[] GetReverseItems(TValue value)
        {
            if (_reverseMap == null)
                throw new InvalidOperationException("reverse map not enabled");
            AssertUtils.IsNotNull(value, "value");

			RWLock.EnterWriteLock();
			try
			{
				InitializeAllItems();
				var result = _reverseMap[value].ToArray();
				return result;
			}
			finally
			{
				RWLock.ExitWriteLock();
			}
        }

        public abstract bool ContainsKey(TKey key);

        protected IEqualityComparer<TKey> KeyComparer
        {
            get { return _keyComparer ?? EqualityComparer<TKey>.Default; }
        }

        protected IEqualityComparer<TValue> ValueComparer
        {
            get 
            {
                return _valueComparer ?? EqualityComparer<TValue>.Default;
            }
        }

        protected void Initialize(TKey key)
        {
			RWLock.EnterWriteLock();
			try
			{
				if (_getSingleValue != null)
					AddItem(key, _getSingleValue(key));
				else
					InitializeAllItems();
			}
			finally
			{
				RWLock.ExitWriteLock();
			}
        }

        private void InitializeAllItems()
        {
            if (!_allValuesSet)
            {
                AssertUtils.IsNotNull(_getAllValues, "getAllValues");

                _getAllValues().ForEach(item => AddItem(item.Key, item.Value));
                _allValuesSet = true;
            }
        }

        protected void AddItem(TKey key, TValue value)
        {
			if (!AllowNullValues && !typeof(TValue).IsValueType)
			{
				object obj = value;
				if (obj == null)
					throw new NotSupportedException("Null values are not allowed. Key: " + key);
			}

        	DoAddItem(key, value);

            if (_reverseMap != null)
                _reverseMap.Add(value, key);
        }

        protected abstract IEnumerable<KeyValuePair<TKey, TValue>> GetCurrentItems();
        protected abstract void DoClear();
		protected abstract TValue[] DoClearKey(TKey key);
        protected abstract void DoAddItem(TKey key, TValue value);
    }
}
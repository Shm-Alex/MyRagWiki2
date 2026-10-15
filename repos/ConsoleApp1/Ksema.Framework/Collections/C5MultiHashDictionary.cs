using System;
using System.Collections.Generic;
using SCG = System.Collections.Generic;

namespace C5
{
	public class MultiHashDictionaryBase<K,V,VS> : HashDictionary<K,VS>
		where VS : class, ICollection<V>, ICloneable
	{
		private readonly IEqualityComparer<V> _valueComparer;
		private int _count;
		private int _keyCount;
		VS _valuesPrototype;

		public MultiHashDictionaryBase() 
			: this(EqualityComparer<K>.Default, EqualityComparer<V>.Default)
		{
		}

		public MultiHashDictionaryBase(IEqualityComparer<K> keyComparer)
			:this(keyComparer, EqualityComparer<V>.Default)
		{
		}

		public MultiHashDictionaryBase(IEqualityComparer<K> keyComparer, 
										IEqualityComparer<V> valueComparer) 
			: base(keyComparer)
		{
			_valueComparer = valueComparer;
			InitializeEvents();
		}

		VS CreateValues()
		{
			if (_valuesPrototype == null)
			{
				Type vsType = typeof(VS);
				_valuesPrototype = (VS)Activator.CreateInstance(vsType, _valueComparer);
			}
			VS values = (VS)_valuesPrototype.Clone();
			return values;
		}

		private void InitializeEvents()
		{
			ItemsAdded +=
				delegate(Object sender, ItemCountEventArgs<KeyValuePair<K, VS>> args)
					{
						VS values = args.Item.Value;
						if (values != null)
						{
							_count += values.Count;
							if (!values.IsEmpty)
								_keyCount += 1;
							values.ItemsAdded += values_IncrementCount;
							values.ItemsRemoved += values_DecrementCount;
							values.CollectionCleared += values_ClearedCount;
						}
					};

			ItemsRemoved +=
				delegate(Object sender, ItemCountEventArgs<KeyValuePair<K, VS>> args)
				{
					VS values = args.Item.Value;
					if (values != null)
					{
						_count -= values.Count;
						if (!values.IsEmpty)
							_keyCount -= 1;
						values.ItemsAdded -= values_IncrementCount;
						values.ItemsRemoved -= values_DecrementCount;
						values.CollectionCleared -= values_ClearedCount;
					}
				};

		}

		private void values_ClearedCount(object sender, ClearedEventArgs eventargs)
		{
			_count -= eventargs.Count;
			if (eventargs.Count != 0)
				_keyCount -= 1;
		}

		private void values_DecrementCount(object sender, ItemCountEventArgs<V> eventargs)
		{
			_count -= eventargs.Count;
			VS values = (VS)sender;
			if (values.IsEmpty && eventargs.Count != 0)
				_keyCount -= 1;
		}

		private void values_IncrementCount(object sender, ItemCountEventArgs<V> eventargs)
		{
			_count += eventargs.Count;
			VS values = (VS)sender;
			if ((values.Count - eventargs.Count)  == 0)
				_keyCount += 1;
		}

		public override void Clear()
		{
			UnbindEvents();
			_count = 0;
			_keyCount = 0;
			base.Clear();
		}

		private void UnbindEvents()
		{
			foreach (KeyValuePair<K,VS> entry in this)
			{
				if (entry.Value != null)
				{
					entry.Value.ItemsAdded -= values_IncrementCount;
					entry.Value.ItemsRemoved -= values_DecrementCount;
					entry.Value.CollectionCleared -= values_ClearedCount;
				}
			}
		}

		public virtual bool Contains(K key, V value)
		{
			VS values;
			if (base.Find(key, out values) && values != null && !values.IsEmpty)
				return values.Contains(value);
			return false;
		}

		public override bool Contains(K key)
		{
			VS values;
			return base.Find(key, out values) && values != null && !values.IsEmpty;
		}

		public virtual void Add(K key, V value)
		{
			VS values = GetOrCreateValues(key);
			values.Add(value);
		}

		private VS GetOrCreateValues(K key)
		{
			VS values;
			if (!base.Find(key, out values) || values == null)
			{
				values = CreateValues();
				this[key] = values;
			}
			return values;
		}

		public override VS this[K key]
		{
			get
			{
				return GetOrCreateValues(key);
			}
			set
			{
				base[key] = value;
			}
		}

		public virtual void RemoveMany(IEnumerable<K> keyList)
		{
			foreach (K key in keyList)
				Remove(key);
		}

		public virtual bool Remove(K key, V value)
		{
			VS values;
			if (base.Find(key, out values) && values != null)
			{
				return values.Remove(value);
			}
			return false;
		}

		public virtual int KeyCount
		{
			get { return _keyCount; }
		}

		public override int Count
		{
			get { return _count; }
		}

		public override ICollectionValue<K> Keys
		{
			get
			{
				return new KeysCollection(this);
			}
		}

		public virtual IEnumerable<KeyValuePair<K, V>> AllEntries
		{
			get 
			{
				foreach (KeyValuePair<K, VS> entry in this)
				{
					if (entry.Value != null)
						foreach (V value in entry.Value)
							yield return new KeyValuePair<K, V>(entry.Key, value);
				}
			}
		}

		public virtual IEnumerable<V> AllValues
		{
			get 
			{
				foreach (KeyValuePair<K, VS> entry in this)
				{
					if (entry.Value != null)
						foreach (V value in entry.Value)
							yield return value;
				}				
			}
		}

		class KeysCollection : CollectionValueBase<K>
		{
			readonly MultiHashDictionaryBase<K, V, VS> _owner;

			public KeysCollection(MultiHashDictionaryBase<K, V, VS> owner)
			{
				_owner = owner;
			}

			public override K Choose()
			{
				foreach (KeyValuePair<K, VS> entry in _owner)
				{
					if (entry.Value != null && !entry.Value.IsEmpty)
						return entry.Key;
				}
				throw new NoSuchItemException();
			}

			public override bool IsEmpty
			{
				get { return _owner._keyCount == 0; }
			}

			public override int Count
			{
				get { return _owner._keyCount; }
			}

			public override Speed CountSpeed
			{
				get { return Speed.Constant; }
			}

			public override IEnumerator<K> GetEnumerator()
			{
				foreach (KeyValuePair<K, VS> entry in _owner)
				{
					if (entry.Value != null && !entry.Value.IsEmpty)
						yield return entry.Key;
				}
			}
		}
	}

	public class MultiHashDictionary<K, V> : MultiHashDictionaryBase<K, V, HashSet<V>>
	{
		public MultiHashDictionary()
		{
		}

		public MultiHashDictionary(IEqualityComparer<K> keyEqualityComparer) : base(keyEqualityComparer)
		{
		}

		public MultiHashDictionary(IEqualityComparer<K> keyComparer, IEqualityComparer<V> valueComparer) : base(keyComparer, valueComparer)
		{
		}
	}
}

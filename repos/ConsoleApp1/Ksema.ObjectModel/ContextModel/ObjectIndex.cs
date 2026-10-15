using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Collections;
using Framework.Utils;

namespace Ksema.ObjectModel.ContextModel
{
    public interface IObjectIndex<TObj>
        where TObj : class
    {
        string Name{get;}
        bool IsUnique{get;}

        void Add(TObj obj);
    	void Remove(TObj obj);
		void Remove(TObj obj, bool isPermanent);
		void Clear();
        void SetObjectLoaded(TObj obj, bool isLoaded);
    }

    public interface IObjectLookup<TKey>
    {
        ICollection this[TKey key] { get; }
        int GetCount(TKey key);
		bool IsKeyLoaded(TKey key);
		void SetKeyLoaded(TKey key, bool loaded);
    }

    public class ObjectIndex<TObj, TKey> : 
        IObjectIndex<TObj>, 
        IObjectLookup<TKey>, 
        ILookup<TKey, TObj>
        where TObj : class
    {
        readonly string _name;
        readonly IIndexer<TObj, TKey> _indexer;
        readonly bool _unique;
        readonly C5.MultiHashDictionary<TKey, TObj> _storage;
		readonly Dictionary<TObj, TKey> _objectKeys;
		readonly HashSet<TKey> _loadedKeys;

        public ObjectIndex(string name, IIndexer<TObj, TKey> aIndexer, bool unique)
        {
            _indexer = aIndexer;
            _unique = unique;
            _name = name;

            if (String.IsNullOrEmpty(name))
                _name = "IX_" + Guid.NewGuid();

            _storage = new C5.MultiHashDictionary<TKey, TObj>(aIndexer.GetKeyComparer());
			_objectKeys = new Dictionary<TObj, TKey>();
			_loadedKeys = new HashSet<TKey>(aIndexer.GetKeyComparer());
        }


        public string Name
        {
            get { return _name; }
        }

        public bool IsUnique
        {
            get { return _unique; }
        }

        public int GetCount(TKey key)
        {
            return Contains(key) ? this[key].Count : 0;
        }

        ICollection IObjectLookup<TKey>.this[TKey key]
        {
			get { return this[key].ToArray(); }
        }

    	IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        public IEnumerator<IGrouping<TKey, TObj>> GetEnumerator()
        {
            foreach (C5.KeyValuePair<TKey, C5.HashSet<TObj>> entry in _storage)
				if(entry.Value != null && !entry.Value.IsEmpty)
					yield return new IndexGroup(entry.Key, entry.Value);
        }


        public bool Contains(TKey key)
        {
            return _storage.Contains(key);
        }

		public bool IsKeyLoaded(TKey key)
		{
			return _loadedKeys.Contains(key);
		}

		public void SetKeyLoaded(TKey key, bool loaded)
		{
			if (loaded)
				_loadedKeys.Add(key);
			else
				_loadedKeys.Remove(key);
		}

    	public int Count
        {
            get { return _storage.Count; }
        }

        IEnumerable<TObj> ILookup<TKey, TObj>.this[TKey key]
        {
            get { return this[key]; }
        }

        public ICollection<TObj> this[TKey key]
        {
            get { return _storage[key]; }
        }

        public void Add(TObj obj)
        {
			AssertUtils.IsNotNull(obj, "object");

            TKey key = _indexer.GetIndexKey(obj);
            if (IsUnique && _storage.Contains(key))
            {
                TObj existingObj = _storage[key].First();
                if(!ReferenceEquals(obj, existingObj))
                    throw new InvalidOperationException(
                        string.Format("Object with the key '{0}' already exists in the unique index '{1}'", key, Name));
            }
            _storage.Add(key, obj);
			_objectKeys[obj] = key;
        }

		public void Update(TObj obj)
		{
			TKey oldKey;
			if (_objectKeys.TryGetValue(obj, out oldKey))
			{
				TKey newKey = _indexer.GetIndexKey(obj);
				if (!_indexer.GetKeyComparer().Equals(oldKey, newKey))
				{
					_storage.Remove(oldKey, obj);
					_storage.Add(newKey, obj);
					_objectKeys[obj] = newKey;
				}
			}
		}

    	public void Remove(TObj obj)
		{
			Remove(obj, false);
		}

    	public void Remove(TObj obj, bool isPermanent)
        {
            TKey key = _indexer.GetIndexKey(obj);
            _storage.Remove(key, obj);

			TKey exKey;
			if (_objectKeys.TryGetValue(obj, out exKey)
				&& !_indexer.GetKeyComparer().Equals(exKey, key))
			{
				_storage.Remove(exKey, obj);
			}

    		_objectKeys.Remove(obj);

			if (!isPermanent)
				_loadedKeys.Remove(key);
        }

        public void SetObjectLoaded(TObj obj, bool isLoaded)
        {
            TKey key = _indexer.GetIndexKey(obj);
            SetKeyLoaded(key, isLoaded);
        }

        public void Clear()
        {
            _storage.Clear();
			_objectKeys.Clear();
			_loadedKeys.Clear();
        }

        class IndexGroup : IGrouping<TKey, TObj>
        {
            readonly ICollection<TObj> _values;

            public IndexGroup(TKey key, ICollection<TObj> values)
            {
                _values = values;
                Key = key;
            }

            public TKey Key { get; private set; }

            public IEnumerator<TObj> GetEnumerator()
            {
                return _values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

        }
    }

}

using System;
using System.Collections;
using System.Collections.Generic;

namespace Framework.Collections
{
	#region Interface

	public interface IIndexer
	{
		object GetIndexKey(object o);
	}

    public interface IIndexer<TObj, TKey> where TObj : class
    {
        TKey GetIndexKey(TObj obj);
        IEqualityComparer<TKey> GetKeyComparer();
    }

	public interface IIndexHolder
	{
		string Name{get;}
		IIndexer Indexer{get;}
	}

	interface IIndexHolderImpl : IIndexHolder
	{
		ICollection Values{get;}
		ICollection GetValues(object key);
		void Add(object key, object value);
		void Remove(object key, object value);
		bool Contains(object key);
		void Clear();
	}

	public interface IMultiIndexedCollection : ICollection
	{
		ICollection Elements{get;}
		bool Contains(object o);
		bool Contains(string indexName, object key);
		ICollection FindByIndex(string indexName, object key);
		ICollection this[string indexName, object key]{get;}

		ICollection Indexes{get;}
	}
	public interface IMultiIndexedList : IMultiIndexedCollection
	{
		void Add(object o);
		int Remove(object o);
		void Clear();

		IIndexHolder AddIndex(IIndexer indexer, string indexName);
		void RemoveIndex(string indexName);
	}
	#endregion

	#region Implementation

	class IndexHolderImpl : IIndexHolderImpl
	{
		static IList __emptyList = new object[0];
		string _name;
		IIndexer _indexer;
		IDictionary _values = new Hashtable();

		public IndexHolderImpl(string name, IIndexer indexer)
		{
			_name = name;
			_indexer = indexer;
		}

		#region IIndexHolder Implementation

		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				_name = value;
			}
		}
		public IIndexer Indexer
		{
			get
			{
				return _indexer;
			}
			set
			{
				_indexer = value;
			}
		}

		#endregion

		#region IIndexHolderImpl Implementation

		public ICollection Values
		{
			get
			{
				return _values.Values;
			}
		}

		public ICollection GetValues(object key)
		{
			ICollection list = _values[key] as ICollection;
			return list != null ? list : __emptyList;
		}

		public void Add(object key, object value)
		{
			IList list = _values[key] as IList;
			if(list == null)
			{
				list = new OrderedList();
				_values.Add(key, list);
			}
			list.Add(value);
		}
		public void Remove(object key, object value)
		{
			IList list = _values[key] as IList;
			if(list != null)
				list.Remove(value);
		}
		public bool Contains(object key)
		{
			return _values.Contains(key);
		}
		public void Clear()
		{
			_values.Clear();
		}

		#endregion
	}
	public class MultiIndexedList : IMultiIndexedList
	{
		int _counter = 0;
		Hashtable _elements = new Hashtable();
		IDictionary _indexes = new Hashtable();

		#region IMultiIndexedList Members

		void SetNewCounter()
		{
			_counter++;
			int n = _counter;
			while(_elements.Contains(_counter.ToString()))
			{
				_counter++;
				if(n == _counter)
					throw new ArgumentException("Переполнение MultiIndexed");
			}
		}

		public void Add(object o)
		{
			SetNewCounter();
			_elements.Add(_counter,o);
			foreach(IIndexHolderImpl index in _indexes.Values)
				index.Add(index.Indexer.GetIndexKey(o), _counter);
		}

		object GetCounter(object o)
		{
			if(o != null)
			{
				IDictionaryEnumerator enumerator = _indexes.GetEnumerator();
				if(enumerator.MoveNext())
				{
					IIndexHolderImpl index = enumerator.Value as IIndexHolderImpl;
					foreach(object number in index.GetValues(index.Indexer.GetIndexKey(o)))
					{
						if(object.Equals(o,_elements[number]))
							return number;
					}
				}
			}
			return null;
		}

		public int Remove(object o)
		{
			object number = GetCounter(o);
			if(number != null)
			{
				foreach(IIndexHolderImpl index in _indexes.Values)
					index.Remove(index.Indexer.GetIndexKey(o), number);
				_elements.Remove(number);
				return 1;
			}
			return 0;
		}

		public void Clear()
		{
			_elements.Clear();
			foreach (IIndexHolderImpl index in _indexes.Values)
				index.Clear();
		}

		void CheckInputData(IIndexer indexer, string indexName)
		{
			if(indexer == null)
				throw new ArgumentException("Тип индекса не задан");
			if(indexName == null || indexName == "")
				throw new ArgumentException("Имя индекса не задано");
			if(_indexes.Contains(indexName))
				throw new ArgumentException("Индекс с таким именем уже есть");
		}

		public IIndexHolder AddIndex(IIndexer indexer, string indexName)
		{
			CheckInputData(indexer, indexName);
			IIndexHolderImpl index = new IndexHolderImpl(indexName, indexer);
			foreach(DictionaryEntry d in _elements)
				index.Add(index.Indexer.GetIndexKey(d.Value),d.Key);
			_indexes.Add(index.Name,index);
			return index;
		}

		public void RemoveIndex(string indexName)
		{
			_indexes.Remove(indexName);
		}

		#endregion

		#region IMultiIndexedCollection Members

		public ICollection Elements
		{
			get
			{
				return _elements.Values;
			}
		}

		public bool Contains(object o)
		{
			return _elements.ContainsValue(o);
		}

		IIndexHolderImpl CheckInputData(string indexName, object key)
		{
			if(indexName == null)
				throw new ArgumentException("Имя индекса не задано");
			if(key == null)
				throw new ArgumentException("Ключ поиска не задан");
			return _indexes[indexName] as IIndexHolderImpl;
		}

		public bool Contains(string indexName, object key)
		{
			IIndexHolderImpl index = CheckInputData(indexName, key);
			return index != null && index.Contains(key);
		}

		public ICollection FindByIndex(string indexName, object key)
		{
			IIndexHolderImpl index = CheckInputData(indexName, key);
			IList list = new ArrayList();
			if(index != null)
				foreach(object o in index.GetValues(key))
					list.Add(_elements[o]);
			return list;
		}

		public ICollection this[string indexName, object key]
		{
			get
			{
				return this.FindByIndex(indexName, key);
			}
		}

		public ICollection Indexes
		{
			get
			{
				return _indexes.Values;
			}
		}

		#endregion

		#region ICollection Members

		public bool IsSynchronized
		{
			get
			{
				return false;
			}
		}

		public int Count
		{
			get
			{
				return _elements.Count;
			}
		}

		public void CopyTo(Array array, int index)
		{
			_elements.Values.CopyTo(array, index);
		}

		public object SyncRoot
		{
			get
			{
				return this;
			}
		}

		#endregion

		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return _elements.Values.GetEnumerator();
		}

		#endregion
	}

	#endregion
}

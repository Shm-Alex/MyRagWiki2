using System;
using System.Collections;

namespace Framework.Collections
{
	public class ReadOnlyDictionary : IDictionary
	{
		IDictionary _source;

		public ReadOnlyDictionary(IDictionary source)
		{
			_source = source;
		}

		#region IDictionary Members

		bool IDictionary.IsReadOnly
		{
			get
			{
				return true;
			}
		}

		public IDictionaryEnumerator GetEnumerator()
		{
			return _source.GetEnumerator();
		}

		public object this[object key]
		{
			get
			{
				return _source[key];
			}
			set
			{
				throw new NotSupportedException("ReadOnlyDictionary только для чтения");
			}
		}

		void IDictionary.Remove(object key)
		{
			throw new NotSupportedException("ReadOnlyDictionary только для чтения");
		}

		public bool Contains(object key)
		{
			return _source.Contains(key);
		}

		void IDictionary.Clear()
		{
			throw new NotSupportedException("ReadOnlyDictionary только для чтения");
		}

		public ICollection Values
		{
			get
			{
				return _source.Values;
			}
		}

		void IDictionary.Add(object key, object value)
		{
			throw new NotSupportedException("ReadOnlyDictionary только для чтения");
		}

		public ICollection Keys
		{
			get
			{
				return _source.Keys;
			}
		}

		bool IDictionary.IsFixedSize
		{
			get
			{
				return true;
			}
		}

		#endregion

		#region ICollection Members

		bool ICollection.IsSynchronized
		{
			get
			{
				return _source.IsSynchronized;
			}
		}

		public int Count
		{
			get
			{
				return _source.Count;
			}
		}

		public void CopyTo(System.Array array, int index)
		{
			_source.CopyTo(array, index);
		}

		object ICollection.SyncRoot
		{
			get
			{
				return _source.SyncRoot;
			}
		}

		#endregion

		#region IEnumerable Members

		IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return (_source as ICollection).GetEnumerator();
		}

		#endregion
	}
}

using System;
using System.Collections;

namespace Framework.Collections
{
	public interface INNDictionary : IDictionary
	{
		ICollection GetValues(object key);
		ICollection GetKeys(object value);
		bool ContainsKey(object key);
		bool ContainsValue(object value);
		bool ContainsAsso(object key, object value);
		void AddAsso(object key, object value);
		void RemoveKeyValues(object key);
		void RemoveValueKeys(object value);
		void RemoveAsso(object key, object value);
	}

	public class NNDictionary : INNDictionary
	{
		IMultiDictionary _keys, _values;

		public NNDictionary()
		{
			_keys = new MultiMap();
			_values = new MultiMap();
		}

		#region INNDictionary

		public ICollection GetValues(object key)
		{
			if(key == null)
				throw new ArgumentNullException("key");

			return _values.GetValues(key);
		}

		public ICollection GetKeys(object value)
		{
			if(value == null)
				throw new ArgumentNullException("value");

			return _keys.GetValues(value);
		}

		public bool ContainsKey(object key)		
		{
			if(key == null)
				throw new ArgumentNullException("key");

			return _values.Contains(key);
		}

		public bool ContainsValue(object value)
		{
			if(value == null)
				throw new ArgumentNullException("value");

			return _keys.Contains(value);
		}

		public bool ContainsAsso(object key, object value)
		{
			if(value == null)
				throw new ArgumentNullException("value");

			foreach(object exValue in GetValues(key))
				if(Object.Equals(exValue, value))
					return true;

			return false;
		}

		public void AddAsso(object key, object value)
		{
			if(ContainsAsso(key, value))
				throw new ArgumentException("asso already exists: key=" + key + "; value=" + value);

			_keys.Add(value, key);
			try
			{
				_values.Add(key, value);
			}
			catch
			{
				_keys.Remove(value, key);
				throw;
			}
		}

		public void RemoveKeyValues(object key)
		{
			if(ContainsKey(key))
			{
				foreach(object value in GetValues(key))
				{
					_keys.Remove(value, key);
				}
				_values.Remove(key);
			}
		}

		public void RemoveValueKeys(object value)
		{
			if(ContainsValue(value))
			{
				foreach(object key in GetKeys(value))
				{
					_values.Remove(key, value);
				}
				_keys.Remove(value);
			}
		}

		public void RemoveAsso(object key, object value)
		{
			if(ContainsAsso(key, value))
			{
				_values.Remove(key, value);
				_keys.Remove(value, key);
			}
		}

		#endregion

		#region IDictionary

		public bool Contains(object key)
		{
			return ContainsKey(key);
		}

		public void Add(object key, object value)
		{
			AddAsso(key, value);
		}

		public void Clear()
		{
			_values.Clear();
			_keys.Clear();
		}

		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return _values.GetEnumerator();
		}

		public void Remove(object key)
		{
			RemoveKeyValues(key);
		}

		public ICollection Keys
		{
			get { return _keys.Values; }
		}

		public ICollection Values
		{
			get { return _values.Values; }
		}

		public bool IsReadOnly
		{
			get { return _values.IsReadOnly; }
		}

		public bool IsFixedSize
		{
			get { return _values.IsFixedSize; }
		}

		public object this[object key]
		{
			get { throw new NotSupportedException(); }
			set { throw new NotSupportedException(); }
		}

		#endregion

		#region ICollection

		public void CopyTo(Array array, int index)
		{
			_values.CopyTo(array, index);
		}

		public int Count
		{
			get { return _values.Count; }
		}

		public object SyncRoot
		{
			get { return this; }
		}

		public bool IsSynchronized
		{
			get { return _values.IsSynchronized; }
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _values.GetEnumerator();
		}

		#endregion
	}
}
using System;
using System.Collections;

namespace Framework.Collections
{
	public interface ITwoWayDictionary : IDictionary
	{
		void RemoveValue(object value);
		bool ContainsValue(object value);
		object Key(object value);
		object Value(object key);
	}


	public class TwoWayDictionary : ITwoWayDictionary
	{
		private IDictionary forward;
		private IDictionary reverse;
		public TwoWayDictionary()
		{
			forward = new Hashtable();
			reverse = new Hashtable();
		}


		public void Add(object key, object value)
		{
			forward.Add(key, value);
			try
			{
				reverse.Add(value, key);
			}
			catch
			{
				forward.Remove(key);
				throw;
			}
		}
		public void Remove(object key)
		{
			if (forward.Contains(key))
			{
				object value = forward[key];
				forward.Remove(key);
				reverse.Remove(value);
			}
		}
		public void RemoveValue(object value)
		{
			if (reverse.Contains(value))
			{
				object key = reverse[value];
				forward.Remove(key);
				reverse.Remove(value);
			}
		}
		public bool Contains(object key)
		{
			return forward.Contains(key);
		}
		public bool ContainsValue(object value)
		{
			return reverse.Contains(value);
		}
		public object this[object key]
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
				throw new NotSupportedException();
			}
		}
		public object Key(object value)
		{
			return reverse[value];
		}
		public object Value(object key)
		{
			return forward[key];
		}
		public ICollection Keys
		{
			get
			{
				return forward.Keys;
			}
		}
		public ICollection Values
		{
			get
			{
				return forward.Values;
			}
		}
		public int Count
		{
			get
			{
				return forward.Count;
			}
		}
		public void Clear()
		{
			forward.Clear();
			reverse.Clear();
		}
		public IDictionaryEnumerator GetEnumerator()
		{
			return forward.GetEnumerator();
		}
		IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}
		public bool IsReadOnly
		{
			get
			{
				return forward.IsReadOnly;
			}
		}
		public bool IsFixedSize
		{
			get
			{
				return forward.IsFixedSize;
			}
		}
		public bool IsSynchronized
		{
			get
			{
				return forward.IsSynchronized;
			}
		}
		public object SyncRoot
		{
			get
			{
				return this;
			}
		}
		public void CopyTo(Array array, int index)
		{
			forward.CopyTo(array, index);
		}
	}
}

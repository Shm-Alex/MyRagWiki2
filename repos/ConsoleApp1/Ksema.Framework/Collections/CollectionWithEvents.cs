using System;
using System.Collections;
using System.Collections.Generic;

namespace Ksema.Framework
{
	public class DataItemEventArgs<T> : EventArgs
	{
		public DataItemEventArgs(T item)
		{
			Item = item;
		}

		public T Item{get;set;}
	}

	public class CollectionWithEvents<T, CT> : ICollection<T>
		where CT : ICollection<T>, new()
	{
		readonly CT _innerList = new CT();

		public event EventHandler<DataItemEventArgs<T>> ItemAdded;
		public event EventHandler<DataItemEventArgs<T>> ItemRemoved;
		public event EventHandler ItemsCleared;
		public event EventHandler ItemsClearing;

		public IEnumerator<T> GetEnumerator()
		{
			return _innerList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(T item)
		{
			_innerList.Add(item);
			OnItemAdded(item);
		}

		public void Clear()
		{
			OnItemsClearing();
			_innerList.Clear();
			OnItemsCleared();
		}

		public bool Contains(T item)
		{
			return _innerList.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_innerList.CopyTo(array, arrayIndex);
		}

		public bool Remove(T item)
		{
			if (_innerList.Remove(item))
			{
				OnItemRemoved(item);
				return true;
			}
			return false;
		}

		public int Count
		{
			get { return _innerList.Count; }
		}

		public bool IsReadOnly
		{
			get { return _innerList.IsReadOnly; }
		}

		private void OnItemAdded(T item)
		{
			if (ItemAdded != null)
				ItemAdded(this, new DataItemEventArgs<T>(item));
		}

		private void OnItemRemoved(T item)
		{
			if (ItemRemoved != null)
				ItemRemoved(this, new DataItemEventArgs<T>(item));
		}

		private void OnItemsClearing()
		{
			if (ItemsClearing != null)
				ItemsClearing(this, EventArgs.Empty);
		}

		private void OnItemsCleared()
		{
			if (ItemsCleared != null)
				ItemsCleared(this, EventArgs.Empty);
		}
	}

	public class LinkedListCollection<T> : CollectionWithEvents<T, LinkedList<T>>
	{
	}
}

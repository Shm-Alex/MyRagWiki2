using System;
using System.Collections;
namespace Framework.Collections
{
	public interface IDeque : ICollection
	{
		void PushHead(object item);
		object PopHead();
		object PeekHead();

		void PushTail(object item);
		object PopTail();
		object PeekTail();
	}
	[Serializable] public class Deque : IDeque, ICloneable
	{
		private const int __defaultCapacity = 10;
		private object[] list;
		private int head = 0;
		private int tail = 0;
		private int count = 0;
		public Deque() : this(__defaultCapacity)
		{
		}
		public Deque(int initialCapacity)
		{
			if (initialCapacity <= 0)
				throw new ArgumentOutOfRangeException("initialCapacity", "Capacity must be positive");
			list = new object[initialCapacity];
		}


		private void CheckFull()
		{
			if (count == list.Length)
			{
				object[] newList = new object[count * 2];
				CopyTo(newList, count / 2);
				list = newList;
				tail = count / 2;
				head = tail + count;
			}
		}
		private void CheckEmpty()
		{
			if (count == 0)
				throw new InvalidOperationException("Deque is empty");
		}


		private void IncHead()
		{
			head++;
			if (head == list.Length)
				head = 0;
			count++;
		}
		private int PeekDecHead()
		{
			int peekHead = head - 1;
			if (peekHead == -1)
				peekHead = list.Length - 1;
			return peekHead;
		}
		private void DecHead()
		{
			head = PeekDecHead();
			count--;
		}
		private void DecTail()
		{
			tail--;
			if (tail == -1)
				tail = list.Length - 1;
			count++;
		}
		private void IncTail()
		{
			tail++;
			if (tail == list.Length)
				tail = 0;
			count--;
		}


		public void PushHead(object item)
		{
			CheckFull();
			list[head] = item;
			IncHead();
		}
		public object PopHead()
		{
			CheckEmpty();
			DecHead();
			object item = list[head];
			list[head] = null;
			return item;
		}
		public object PeekHead()
		{
			CheckEmpty();
			return list[PeekDecHead()];
		}
		public void PushTail(object item)
		{
			CheckFull();
			DecTail();
			list[tail] = item;
		}
		public object PopTail()
		{
			CheckEmpty();
			object item = list[tail];
			list[tail] = null;
			IncTail();
			return item;
		}
		public object PeekTail()
		{
			CheckEmpty();
			return list[tail];
		}


		public void CopyTo(Array array, int index)
		{
			if (array == null)
				throw new ArgumentNullException();
			if (index < 0)
				throw new ArgumentOutOfRangeException();
			if (array.Rank > 1 || array.Length < index + count)
				throw new ArgumentException();
			if (head > tail)
				Array.Copy(list, tail, array, index, count);
			else if (head <= tail && count > 0)
			{
				Array.Copy(list, tail, array, index, list.Length - tail);
				Array.Copy(list, 0, array, index + list.Length - tail, head);
			}
		}
		public int Count
		{
			get
			{
				return count;
			}
		}
		public object SyncRoot
		{
			get
			{
				return this;
			}
		}
		public bool IsSynchronized
		{
			get
			{
				return false;
			}
		}
		/// <summary>
		/// Iterates through immutable copy of this deque
		/// </summary>
		public IEnumerator GetEnumerator()
		{
			object[] copy = new object[count];
			CopyTo(copy, 0);
			return copy.GetEnumerator();
		}
		public object Clone()
		{
			Deque clone = new Deque(list.Length);
			clone.tail = tail;
			clone.head = head;
			clone.count = count;
			list.CopyTo(clone.list, 0);
			return clone;
		}
	}
}

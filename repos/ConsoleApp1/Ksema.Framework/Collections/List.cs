using System;
using System.Collections;
using System.Runtime.Serialization;


namespace Framework.Collections
{
	
	public class ListBase : OnChangedEvents, ICollectionBase
	{
		
		private ListElement NULL = new ListElement("NULL");

		private int count;



		protected ListBase() : base()
		{
			((ICollectionBase)this).ClearAll();
		}




		Element ICollectionBase.First()
		{
			if (NULL.Next == NULL) return(null);
			return(NULL.Next);
		}


		Element ICollectionBase.Last()
		{
			if (NULL.Prev == NULL) return(null);
			return(NULL.Prev);
		}


		Element ICollectionBase.Next(Element X)
		{
			ListElement Y = ((ListElement) X).Next;
			if (Y == NULL) return(null);
			return(Y);
		}


		Element ICollectionBase.Prev(Element X)
		{
			ListElement Y = ((ListElement) X).Prev;
			if (Y == NULL) return(null);
			return(Y);
		}




		void ICollectionBase.ClearAll()
		{
			CallOnClear();
			NULL.Next = NULL;
			NULL.Prev = NULL;
			count = 0;
			CallOnClearComplete();
		}


		int ICollectionBase.DeleteItem(Element X, int i)
		{
			ListElement Y = (ListElement) X;
			CheckData(X.Key);
			CallOnRemove(i, X.Key);
			Y.Prev.Next = Y.Next;
			Y.Next.Prev = Y.Prev;
			Y.Next = null;
			count -= 1;
			CallOnRemoveComplete(i, X.Key);
			return(1);
		}


		void ICollectionBase.ChangeItem(Element X, object key, int i)
		{
			object Save = X.Key;
			CheckData(key);
			CallOnSet(i, Save, key);
			X.Key = key;
			CallOnSetComplete(i, Save, key);
		}


		void ICollectionBase.InsertItemAt(object key, int i)
		{
			CheckData(key);
			CallOnInsert(i, key);
			ListElement Z, Y = new ListElement(key);
			if (i == count) Z = NULL; else Z = (ListElement)(((ICollectionBase) this).Select(ref i));
			Y.Next = Z;
			Y.Prev = Z.Prev;
			Z.Prev = Y;
			Y.Prev.Next = Y;
			count += 1;
			CallOnInsertComplete(i, key);
		}


		int ICollectionBase.AddItem(object key)
		{
			((ICollectionBase) this).InsertItemAt(key, count);
			return(count - 1);
		}




		Element ICollectionBase.Found(object key, out int i)
		{
			ListElement Z = NULL.Next;
			i = 0;
			while (Z != NULL)
			{
				if (Z.Key.Equals(key)) break;
				Z = Z.Next;
				i++;
			}
			if (Z == NULL) return(null);
			return(Z);
		}


		Element ICollectionBase.Select(ref int i)
		{
			if ((i >= count) || (i < 0))
				throw(new ArgumentOutOfRangeException());
			ListElement X;
			if (i + 1 <= count / 2)
			{
				X = NULL.Next;
				for(int j = 1; j <= i; j++)
					X = X.Next;
			}
			else
			{
				X = NULL.Prev;
				int k = count - 1 - i;
				for(int j = 1; j <= k; j++)
					X = X.Prev;
			}
			return(X);
		}


		int ICollectionBase.Rank(Element X)
		{
			if (X == NULL) return(-1);
			ListElement Z = NULL.Next, Y = (ListElement) X;
			int i = 0;
			while (Z != Y)
			{
				Z = Z.Next;
				i++;
			}
			return(i);
		}


		int ICollectionBase.Number
		{
			get
			{
				return(count);
			}
		}




		bool ICollectionBase.IsValid(Element X)
		{
			return(((ListElement) X).Next != null);
		}


		protected void CheckData(object key)
		{
			if (key == null) throw(new ArgumentNullException());
			CallOnValidate(key);
		}


	}


	[Serializable] public class List : ListBase, IList, ICollection, IEnumerable, ICloneable, ISerializable
	{

		public List() : base() {}



		object ICloneable.Clone()
		{
			List clone = new List();
			foreach(object key in this)
				clone.Add(key);
			return clone;
		}


		public List Clone()
		{
			return (List) ((ICloneable)this).Clone();
		}



		public int Add(object key)
		{
			return(((ICollectionBase) this).AddItem(key));
		}


		public void Clear()
		{
			((ICollectionBase) this).ClearAll();
		}


		public bool Contains(object key)
		{
			int i;
			if (((ICollectionBase) this).Found(key, out i) == null) return(false);
			return(true);
		}


		public int IndexOf(object key)
		{
			int i;
			if (((ICollectionBase) this).Found(key, out i) == null) return(-1);
			return(i);
		}


		public void Insert(int i, object key)
		{
			((ICollectionBase) this).InsertItemAt(key, i);
		}


		public void Remove(object key)
		{
			int i;
			Element Z = ((ICollectionBase) this).Found(key, out i);
			if (Z == null) return;
			((ICollectionBase) this).DeleteItem(Z, i);
		}


		public void RemoveAt (int i)
		{
			((ICollectionBase) this).DeleteItem(((ICollectionBase) this).Select(ref i), i);
		}


		public void CopyTo(Array array, int i)
		{
			if (array == null) throw(new ArgumentNullException());
			if (i < 0) throw (new ArgumentOutOfRangeException());
			if ((array.Rank > 1) || (array.Length < i + ((ICollectionBase) this).Number)) 
				throw(new ArgumentException());
			int j = 0;
			foreach(object key in this)
			{
				array.SetValue(key, i+j);
				j++;
			}
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return(GetEnumerator());
		}
	
	
		public IAdvancedEnumerator GetEnumerator()
		{
			return(new Enumerator(this));
		}




		public object this[int i]
		{
			get
			{
				return(((ICollectionBase) this).Select(ref i).Key);
			}
			set
			{
				((ICollectionBase) this).ChangeItem(((ICollectionBase) this).Select(ref i), value, i );
			}
		}


		public int Count
		{
			get
			{
				return(((ICollectionBase) this).Number);
			}
		}


		public bool IsReadOnly
		{
			get
			{
				return(false);
			}
		}


		public bool IsFixedSize
		{
			get
			{
				return(false);
			}
		}


		public bool IsSynchronized
		{
			get
			{
				return(false);
			}
		}


		public object SyncRoot
		{
			get
			{
				return(this);
			}
		}




		#region ISerializable Members

		public List(SerializationInfo info, StreamingContext context)
		{
			foreach (SerializationEntry entry in info)
				this.Add(entry.Value);
		}


		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			int i = 0;
			foreach (object item in this)
				info.AddValue((i++).ToString(), item);
		}


		#endregion



		public object[] Items
		{
			get
			{
				object[] items = new object[Count];
				CopyTo(items, 0);
				return items;
			}
		}
	}


}

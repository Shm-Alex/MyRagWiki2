using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.Serialization;


namespace Framework.Collections
{
	
	public class TreeBase : OnChangedEvents, IMultiCollectionBase
	{
		
		private TreeElement root;
		private TreeElement NULL = new TreeElement("NULL", Colors.black);
		protected static string __comparer = "comparer";
		protected IComparer comparer;



		protected TreeBase(IComparer _comparer) : base()
		{
			comparer = _comparer != null ? _comparer : new StandardComparer();
			((ICollectionBase)this).ClearAll();
		}




		Element Minimum(TreeElement X)
		{
			while (X.Left != NULL) X = X.Left;
			return(X);
		}


		Element Maximum(TreeElement X)
		{
			while (X.Right != NULL) X = X.Right;
			return(X);
		}
		

		void LeftRotate(TreeElement X)
		{
			TreeElement Y = X.Right;
			X.Right = Y.Left;
			if (Y.Left != NULL) Y.Left.Parent = X;
			Y.Parent = X.Parent;
			if (X.Parent == null)
			{
				root = Y;
			}
			else
			{
				if (X == X.Parent.Left)
					X.Parent.Left = Y;
				else
					X.Parent.Right = Y;
			}
			Y.Left = X;
			X.Parent = Y;
			Y.Size = X.Size;
			X.Size = X.Left.Size + X.Right.Size + X.Quantity;
		}


		void RightRotate(TreeElement X)
		{
			TreeElement Y = X.Left;
			X.Left = Y.Right;
			if (Y.Right != NULL) Y.Right.Parent = X;
			Y.Parent = X.Parent;
			if (X.Parent == null)
			{
				root = Y;
			}
			else
			{
				if (X == X.Parent.Left)
					X.Parent.Left = Y;
				else
					X.Parent.Right = Y;
			}
			Y.Right = X;
			X.Parent = Y;
			Y.Size = X.Size;
			X.Size = X.Left.Size + X.Right.Size + X.Quantity;
		}


		void AddFixup(TreeElement X)
		{
			while ((X != root) && (X.Parent.Color == Colors.red))
			{
				if (X.Parent == X.Parent.Parent.Left)
				{
					TreeElement Y = X.Parent.Parent.Right;
					if (Y.Color == Colors.red)
					{
						X.Parent.Color = Colors.black;
						Y.Color = Colors.black;
						X.Parent.Parent.Color = Colors.red;
						X = X.Parent.Parent;
					}
					else
					{
						if (X == X.Parent.Right)
						{
							X = X.Parent;
							LeftRotate(X);
						}
						X.Parent.Color = Colors.black;
						X.Parent.Parent.Color = Colors.red;
						RightRotate(X.Parent.Parent);
					}
				}
				else
				{
					TreeElement Y = X.Parent.Parent.Left;
					if (Y.Color == Colors.red)
					{
						X.Parent.Color = Colors.black;
						Y.Color = Colors.black;
						X.Parent.Parent.Color = Colors.red;
						X = X.Parent.Parent;
					}
					else
					{
						if (X == X.Parent.Left)
						{
							X = X.Parent;
							RightRotate(X);
						}
						X.Parent.Color = Colors.black;
						X.Parent.Parent.Color = Colors.red;
						LeftRotate(X.Parent.Parent);
					}
				}
			}
			root.Color = Colors.black;
		}
		

		void DeleteFixup(TreeElement X)
		{
			while ((X != root) && (X.Color == Colors.black))
			{
				if (X == X.Parent.Left)
				{
					TreeElement W = X.Parent.Right;
					if (W.Color == Colors.red)
					{
						W.Color = Colors.black;
						X.Parent.Color = Colors.red;
						LeftRotate(X.Parent);
						W = X.Parent.Right;
					}
					if ((W.Left.Color == Colors.black) && (W.Right.Color == Colors.black))
					{
						W.Color = Colors.red;
						X = X.Parent;
					}
					else
					{
						if (W.Right.Color == Colors.black)
						{
							W.Left.Color = Colors.black;
							W.Color = Colors.red;
							RightRotate(W);
							W = X.Parent.Right;
						}
						W.Color = X.Parent.Color;
						X.Parent.Color = Colors.black;
						W.Right.Color = Colors.black;
						LeftRotate(X.Parent);
						X = root;
					}
				}
				else
				{
					TreeElement W = X.Parent.Left;
					if (W.Color == Colors.red)
					{
						W.Color = Colors.black;
						X.Parent.Color = Colors.red;
						RightRotate(X.Parent);
						W = X.Parent.Left;
					}
					if ((W.Left.Color == Colors.black) && (W.Right.Color == Colors.black))
					{
						W.Color = Colors.red;
						X = X.Parent;
					}
					else
					{
						if (W.Left.Color == Colors.black)
						{
							W.Right.Color = Colors.black;
							W.Color = Colors.red;
							LeftRotate(W);
							W = X.Parent.Left;
						}
						W.Color = X.Parent.Color;
						X.Parent.Color = Colors.black;
						W.Left.Color = Colors.black;
						RightRotate(X.Parent);
						X = root;
					}
				}
			}
			X.Color = Colors.black;
		}
		



		Element ICollectionBase.First()
		{
			if (root == NULL) return(null);
			return(Minimum(root));
		}


		Element ICollectionBase.Last()
		{
			if (root == NULL) return(null);
			return(Maximum(root));
		}


		Element ICollectionBase.Next(Element X)
		{
			TreeElement Z = (TreeElement) X, Y = Z.Parent;
			if (Z == NULL) return(null);
			if (Z.Right != NULL)
				return(Minimum(Z.Right));
			while ((Y != null) && (Z == Y.Right))
			{
				Z = Y;
				Y = Y.Parent;
			}
			return(Y);
		}


		Element ICollectionBase.Prev(Element X)
		{
			TreeElement Z = (TreeElement) X, Y = Z.Parent;
			if (Z == NULL) return(null);
			if (Z.Left != NULL)
				return(Maximum(Z.Left));
			while ((Y != null) && (Z == Y.Left))
			{
				Z = Y;
				Y = Y.Parent;
			}
			return(Y);
		}




		void ICollectionBase.ClearAll()
		{
			CallOnClear();
			root = NULL;
			CallOnClearComplete();
		}


		void IMultiCollectionBase.DeleteInternalItem(Element X, int index, int i)
		{
			Element Y = ((ICollectionBase) X.Key).Select(ref i);
			CheckData(Y.Key);
			CallOnRemove(index, Y.Key);
			((ICollectionBase) X.Key).DeleteItem(Y, i);
			TreeElement Z = (TreeElement) X;
			Z.Quantity -= 1;
			while(Z != null)
			{
				Z.Size -= 1;
				Z = Z.Parent;
			}
			CallOnRemoveComplete(index, Y.Key);
		}


		int ICollectionBase.DeleteItem(Element W, int i)
		{
			if (IsMulti)
			{
				int j = i;
				foreach (object key in (IEnumerable) W.Key)
				{
					CheckData(key);
					CallOnRemove(j++, key);
				}
			}
			else
			{
				CheckData(W.Key);
				CallOnRemove(i, W.Key);
			}
			TreeElement 
					Z = (TreeElement) W,
					Y = ((Z.Left == NULL) || (Z.Right == NULL)) ? Z : 
												(TreeElement)((ICollectionBase) this).Next(Z),
					X = (Y.Left != NULL) ? Y.Left : Y.Right;
			X.Parent = Y.Parent;
			if (Y.Parent == null)
			{
				root = X;
			}
			else
			{
				if (Y == Y.Parent.Left)
					Y.Parent.Left = X;
				else
					Y.Parent.Right = X;
			};
			bool NeedFixup = (Y.Color == Colors.black);
			if (Y != Z)
			{
				Y.Left = Z.Left;
				Y.Right = Z.Right;
				Y.Parent = Z.Parent;
				Z.Left.Parent = Y;
				if (Z.Right != NULL) Z.Right.Parent = Y;
				if (Z.Parent != null) 
				{
					if (Z == Z.Parent.Left)
						Z.Parent.Left = Y;
					else
						Z.Parent.Right = Y;
				}
				else
				{
					root = Y;
				}
				Y.Color = Z.Color;
				Y.Size = Z.Size;
				if (NULL.Parent == Z) NULL.Parent = Y;
			}
			Z.Right = null;
			Z = X.Parent;
			if (Z != null)
			{
				if (Z != Y.Parent)
					while (Z != Y)
					{
						Z.Size -= Y.Quantity;
						Z = Z.Parent;
					};
				while (Z != null)
				{
					Z.Size -= ((TreeElement) W).Quantity;
					Z = Z.Parent;
				}
			}
			if (NeedFixup) DeleteFixup(X);
			if (IsMulti)
			{
				foreach (object key in (IEnumerable) W.Key)
				{
					CallOnRemoveComplete(i++, key);
				}
			}
			else
			{
				CallOnRemoveComplete(i, W.Key);
			}
			return(((TreeElement) W).Quantity);
		}


		void ICollectionBase.ChangeItem(Element X, object key, int i)
		{
			throw(new InvalidOperationException());
		}


		void ICollectionBase.InsertItemAt(object key, int i)
		{
			throw(new InvalidOperationException());
		}


		int ICollectionBase.AddItem(object key)
		{
			int Compare = 1, r = 0;
			TreeElement X = root, Y = null;
			object PrimaryKey = GetPrimaryKey(key);
			while (X != NULL)
			{
				if (Compare > 0)
				{ r += X.Left.Size + X.Quantity; }
				else
				{ r -= X.Right.Size + Y.Quantity; }
				Y = X;
				object otherPK = GetPrimaryKey(GetListEntry(X.Key, 0));
				Compare = comparer.Compare(PrimaryKey, otherPK);
				if (Compare > 0)
					X = X.Right;
				else
					if (Compare < 0)
					X = X.Left;
				else
				{
					if (!IsMulti) throw(new ArgumentException());
					break;
				}
			}
			CheckData(key);
			CallOnInsert(r, key);
			if (Compare == 0)
			{
				((List) X.Key).Add(key);
			}
			else
			{
				X = new TreeElement(GenerateList(key), Colors.red);
				X.Left = NULL;
				X.Right = NULL;
				X.Parent = Y;
				X.Size = 1;				
				if (Y == null)
					root = X;
				else
				{
					if (Compare > 0)
						Y.Right = X;
					else
						Y.Left = X;
				}
			}
			X.Quantity += 1;
			while (Y != null)
			{
				Y.Size += 1;
				Y = Y.Parent;
			}
			if (Compare != 0) AddFixup(X);
			CallOnInsertComplete(r, key);
			return(r);
		}




		Element ICollectionBase.Found(object key, out int i)
		{
			int Compare = 1;
			i = 0;
			TreeElement Z = root;
			while (Z != NULL) 
			{
				if (Compare > 0)
				{ i += Z.Left.Size + Z.Quantity; }
				else
				{ i -= Z.Right.Size + Z.Parent.Quantity; }
				object otherPK = GetPrimaryKey(GetListEntry(Z.Key, 0));
				Compare = comparer.Compare(key, otherPK);
				if (Compare > 0)
					Z = Z.Right;
				else
					if (Compare < 0)
					Z = Z.Left;
				else
					if (Compare == 0)
					break;
			}
			if (Z == NULL) return(null);
			i -= Z.Quantity;
			return(Z);
		}


		Element ICollectionBase.Select(ref int i)
		{
			if ((i >= root.Size) || (i < 0)) throw(new ArgumentOutOfRangeException());
			int left, right;
			TreeElement X = root;
			while(true)
			{
				left = X.Left.Size;
				right = left + X.Quantity - 1;
				if (i < left)
				{
					X = X.Left;
				}
				else
					if (i > right)
				{
					X = X.Right;
					i -= right + 1;
				}
				else
				{
					i -= left;
					return(X);
				}
			}
		}


		int ICollectionBase.Rank(Element X)
		{
			TreeElement Z = (TreeElement) X;
			if (Z == NULL) return(-1);
			int r = Z.Left.Size;
			while (Z != root)
			{
				if (Z == Z.Parent.Right)
					r = r + Z.Parent.Left.Size + Z.Parent.Quantity;
				Z = Z.Parent;
			}
			return(r);
		}


		int IMultiCollectionBase.GetCount(object key)
		{
			int i;
			return(((TreeElement)((ICollectionBase) this).Found(key, out i)).Quantity);
		}

		
		int ICollectionBase.Number
		{
			get
			{
				return(root.Size);
			}
		}




		bool ICollectionBase.IsValid(Element X)
		{
			return(((TreeElement) X).Right != null);
		}




		protected void CheckData(object key)
		{
			if (GetPrimaryKey(key) == null) throw(new ArgumentNullException());
			CallOnValidate(key);
		}


		protected virtual object GenerateList(object key)
		{
			return(key);
		}


		protected virtual object GetListEntry(object key, int index)
		{
			return(key);
		}


		protected virtual object GetPrimaryKey(object key)
		{
			return(key);
		}


		protected virtual bool IsMulti
		{
			get
			{
				return(false);
			}
		}


		[Conditional("DEBUG")] public void Check()
		{
			int c = 0;
			TreeElement X = root;
			if (X.Color != Colors.black) throw(new InvalidProgramException());
			while(X != NULL)
			{
				if (X.Color == Colors.black) c++; 
				X = X.Left;
			}
			int i = 0;
			CheckAll(root, i, c);
		}


		[Conditional("DEBUG")] private void CheckAll(TreeElement X, int i, int c)
		{
			if (X == NULL)
			{
				if (i != c) throw(new InvalidProgramException());
			}
			else
			{
				if ((X.Color==Colors.red)&&((X.Left.Color==Colors.red)||(X.Right.Color==Colors.red))
					|| ((IsMulti)&&(X.Quantity != ((List) X.Key).Count))
					|| ((!IsMulti)&&(X.Quantity !=1))
					|| (X.Size != X.Quantity+X.Left.Size+X.Right.Size)
					|| ((X.Left != NULL)&&(X.Left.Parent != X))
					|| ((X.Right != NULL)&&(X.Right.Parent != X)))
					throw(new InvalidProgramException());
				if (X.Color == Colors.black) i++;
				CheckAll(X.Left, i, c);
				CheckAll(X.Right, i, c);
			}
		}


	}


	[Serializable] public class OrderedList : TreeBase, IList, ICollection, IEnumerable, ICloneable, ISerializable
	{

		public OrderedList(IComparer comparer) : base(comparer) {}


		public OrderedList() : base(null) {}



		object ICloneable.Clone()
		{
			OrderedList list = new OrderedList();
			foreach(object key in this)
				list.Add(key);
			return list;
		}


		public OrderedList Clone()
		{
			return (OrderedList) ((ICloneable)this).Clone();
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


		public int GetCount(object key)
		{
			return(((IMultiCollectionBase) this).GetCount(key));
		}


		public void Insert(int i, object key)
		{
			throw(new InvalidOperationException());
		}


		void IList.Remove(object key)
		{
			Remove(key);
		}
		

		public int Remove(object key)
		{
			int i;
			Element Z = ((ICollectionBase) this).Found(key, out i);
			if (Z == null) return(0);
			return(((ICollectionBase) this).DeleteItem(Z, i));
		}


		public void RemoveAt (int i)
		{
			int j = i;
			TreeElement X = (TreeElement) ((ICollectionBase) this).Select(ref j);
			if (X.Quantity == 1)
			{
				((ICollectionBase) this).DeleteItem(X, i);
			}
			else
			{
				((IMultiCollectionBase) this).DeleteInternalItem(X, i, j);
			}
		}


		public void CopyTo(Array array, int i)
		{
			if (array == null) throw(new ArgumentNullException());
			if (i < 0) throw (new ArgumentOutOfRangeException());
			if ((array.Rank > 1) || (array.Length < i + Count)) throw(new ArgumentException());
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
			return(new MultiEnumerator(this));
		}




		public object this[int i]
		{
			get
			{
				return(((List) ((ICollectionBase) this).Select(ref i).Key)[i]);
			}
			set
			{
				throw(new InvalidOperationException());
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




		protected override object GetListEntry(object key, int index)
		{
			return(((List) key)[index]);
		}


		protected override object GenerateList(object key)
		{
			List list = new List();
			list.Add(key);
			return(list);
		}


		protected override bool IsMulti
		{
			get
			{
				return(true);
			}
		}




		#region ISerializable Members

		public OrderedList(SerializationInfo info, StreamingContext context) : base((IComparer)info.GetValue(__comparer, typeof(IComparer)))
		{
			foreach (SerializationEntry entry in info)
				if (entry.Name != __comparer)
					this.Add(entry.Value);
		}


		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			int i = 0;
			foreach (object item in this)
				info.AddValue((i++).ToString(), item);
			info.AddValue(__comparer, comparer);
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


	[Serializable] public class Map : TreeBase, IDictionary, IAdvancedDictionary, ICollection, IEnumerable, ICloneable, IDictionaryCollectionBase, ISerializable
	{

		public Map(IComparer comparer) : base(comparer) {}


		public Map() : base(null) {}



		object ICloneable.Clone()
		{
			Map clone = Activator.CreateInstance(this.GetType()) as Map;
			foreach (DictionaryEntry entry in this)
				clone.Add(entry.Key, entry.Value);
			return clone;
		}


		public Map Clone()
		{
			return (Map) ((ICloneable)this).Clone();
		}



		void IDictionary.Add(object key, object data)
		{
			Add(key, data);
		}


		public int Add(object key, object data)
		{
			return(((ICollectionBase) this).AddItem(new DictionaryEntry(key, data)));
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


		void IDictionary.Remove(object key)
		{
			Remove(key);
		}


		public int Remove(object key)
		{
			int i;
			Element Z = ((ICollectionBase) this).Found(key, out i);
			if (Z == null) return(0);
			return(((ICollectionBase) this).DeleteItem(Z, i));
		}


		public void RemoveAt (int i)
		{
			int j = i;
			TreeElement X = (TreeElement) ((ICollectionBase) this).Select(ref j);
			if (X.Quantity == 1)
			{
				((ICollectionBase) this).DeleteItem(X, i);
			}
			else
			{
				((IMultiCollectionBase) this).DeleteInternalItem(X, i, j);
			}
		}


		public void CopyTo(Array array, int i)
		{
			if (array == null) throw(new ArgumentNullException());
			if (i < 0) throw (new ArgumentOutOfRangeException());
			if ((array.Rank > 1) || (array.Length < i + Count)) throw(new ArgumentException());
			int j = 0;
			foreach(DictionaryEntry Entry in this)
			{
				array.SetValue(Entry, i+j);
				j++;
			}
		}


		public ICollection Keys
		{
			get
			{
				object[] array = new object[Count];
				int j = 0;
				foreach(DictionaryEntry Entry in this)
				{
					array.SetValue(Entry.Key, j);
					j++;
				}
				return(array);
			}
		}


		public ICollection Values
		{
			get
			{
				object[] array = new object[Count];
				int j = 0;
				foreach(DictionaryEntry Entry in this)
				{
					array.SetValue(Entry.Value, j);
					j++;
				}
				return(array);
			}
		}


		public object Key(int index)
		{
			return(Entry(index).Key);
		}

		
		public object Value(int index)
		{
			return(Entry(index).Value);
		}


		public DictionaryEntry Entry(int index)
		{
			return((DictionaryEntry) GetListEntry(((ICollectionBase) this).Select(ref index).Key, 
														index));
		}
		

		public void SetValue(int index, object data)
		{
			int i = index;
			Element X = ((ICollectionBase) this).Select(ref i);
			((IDictionaryCollectionBase) this).ChangeValue(X, data, index);
		}


		IEnumerator IEnumerable.GetEnumerator()
		{
			return(GetEnumerator());
		}


		IDictionaryEnumerator IDictionary.GetEnumerator()
		{
			return(GetEnumerator());
		}
	
	
		public virtual IAdvancedDictionaryEnumerator GetEnumerator()
		{
			return(new DictionaryEnumerator(this));
		}




		public object this[object key]
		{
			get
			{
				int i;
				Element Z = ((ICollectionBase) this).Found(key, out i);
				if (Z == null) //throw(new ArgumentException());
					return null;
				else
					return(((DictionaryEntry) GetListEntry(Z.Key, 0)).Value);
			}
			set
			{
				int i;
				Element Z = ((ICollectionBase) this).Found(key, out i);
				if (Z == null) //throw(new ArgumentException());
					Add(key, value);
				else
					((IDictionaryCollectionBase)this).ChangeValue(Z, value, i);
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




		protected override object GetPrimaryKey(object key)
		{
			return(((DictionaryEntry) key).Key);
		}




		void IDictionaryCollectionBase.ChangeValue(Element X, object Value, int i)
		{
			int j = i - ((ICollectionBase) this).Rank(X);
			Element Y = (IsMulti) ? ((ICollectionBase) X.Key).Select(ref j) : X;
			DictionaryEntry Old = (DictionaryEntry) Y.Key,
							New = new DictionaryEntry(Old.Key, Value);
			CheckData(New);
			CallOnSet(i, Old, New);
			Y.Key = New;
			CallOnSetComplete(i, Old, New);
		}



		#region ISerializable Members

		public Map(SerializationInfo info, StreamingContext context) : base((IComparer)info.GetValue(__comparer, typeof(IComparer)))
		{
			foreach (SerializationEntry item in info)
				if (item.Name != __comparer)
				{
					ObjectPair inner = (ObjectPair) item.Value;
					this.Add(inner.First, inner.Second);
				}
		}


		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			int i = 0;
			foreach (DictionaryEntry entry in this)
			{
				ObjectPair inner = new ObjectPair(entry.Key, entry.Value);
				info.AddValue((i++).ToString(), inner, typeof(ObjectPair));
			}
			info.AddValue(__comparer, comparer);
		}


		#endregion
	}


	[Serializable] public class MultiMap : Map, IMultiDictionary, IDictionary, IAdvancedDictionary, ICollection, IEnumerable, ICloneable, IDictionaryCollectionBase, ISerializable
	{

		public MultiMap(IComparer comparer) : base(comparer) {}


		public MultiMap() : base() {}



		public new MultiMap Clone()
		{
			return (MultiMap) ((ICloneable)this).Clone();
		}



		public int Remove(object key, object value)
		{
			IAdvancedDictionaryEnumerator iterator = FindFirst(key);
			int count = 0;
			while (comparer.Compare(iterator.Key, key) == 0)
			{
				if (Object.Equals(iterator.Value, value))
				{
					iterator.Remove();
					count++;
				}
				if (!iterator.MoveNext())
					break;
			}
			return count;
		}


		public int GetCount(object key)
		{
			return(((IMultiCollectionBase) this).GetCount(key));
		}


		public ICollection UniqueKeys
		{
			get
			{
				List list = new List();
				Element E = ((ICollectionBase) this).First();
				while (E != null)
				{
					list.Add(GetPrimaryKey(GetListEntry(E.Key, 0)));
					E = ((ICollectionBase) this).Next(E);
				}
				return(list);
			}
		}


		public ICollection GetValues(object key)
		{
			List list = new List();
			int i;
			Element E = ((ICollectionBase) this).Found(key, out i);
			if (E != null)
				foreach(DictionaryEntry A in (IEnumerable) E.Key)
				{
					list.Add(A.Value);
				}
			return(list);
		}


		public override IAdvancedDictionaryEnumerator GetEnumerator()
		{
			return(new MultiDictionaryEnumerator(this));
		}


		public IAdvancedDictionaryEnumerator FindFirst(object key)
		{
			int i;
			Element X = ((ICollectionBase) this).Found(key, out i);
			MultiDictionaryEnumerator E = new MultiDictionaryEnumerator(this);
			E.outerElement = X;
			E.state = States.Positioned;
			E.innerCollection = (ICollectionBase) X.Key;
			E.innerElement = ((ICollectionBase) X.Key).First();
			return(E);
		}


		public IAdvancedDictionaryEnumerator FindLast(object key)
		{
			int i;
			Element X = ((ICollectionBase) this).Found(key, out i);
			MultiDictionaryEnumerator E = new MultiDictionaryEnumerator(this);
			E.outerElement = X;
			E.state = States.Positioned;
			E.innerCollection = (ICollectionBase) X.Key;
			E.innerElement = ((ICollectionBase) X.Key).Last();
			return(E);
		}



		protected override object GetListEntry(object key, int index)
		{
			return(((List) key)[index]);
		}


		protected override object GenerateList(object key)
		{
			List list = new List();
			list.Add(key);
			return(list);
		}


		protected override bool IsMulti
		{
			get
			{
				return(true);
			}
		}



		#region ISerializable Members

		public MultiMap(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}


		#endregion
	}


	[Serializable] public class ObjectPair : ISerializable
	{
		private object first;
		private object second;
		public ObjectPair()
		{
		}

		public ObjectPair(object _first, object _second)
		{
			first = _first;
			second = _second;
		}


		public object First
		{
			get{return first;}
			set{first = value;}
		}

		public object Second
		{
			get{return second;}
			set{second = value;}
		}


		#region ISerializable Members
		public ObjectPair(SerializationInfo info, StreamingContext context)
		{
			first = info.GetValue("f", typeof(object));
			second = info.GetValue("s", typeof(object));
		}

		public void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("f", first, first != null ? first.GetType() : typeof(object));
			info.AddValue("s", second, second != null ? second.GetType() : typeof(object));
		}
		#endregion
	}
}
